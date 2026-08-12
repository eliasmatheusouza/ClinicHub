import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = (__ENV.BASE_URL || 'http://host.docker.internal:8082').replace(/\/$/, '');
const profile = __ENV.PERF_PROFILE || 'smoke';
const userEmail = __ENV.PERF_USER_EMAIL;
const userPassword = __ENV.PERF_USER_PASSWORD;
const accessToken = __ENV.PERF_ACCESS_TOKEN;
const runId = `${Date.now()}-${Math.floor(Math.random() * 1000000000)}`;

const profiles = {
  smoke: { vus: 2, iterations: 1 },
  baseline: { vus: 10, iterations: 5 }
};

if (!profiles[profile] || !userEmail || !userPassword) {
  throw new Error('Defina PERF_PROFILE (smoke/baseline), PERF_USER_EMAIL e PERF_USER_PASSWORD.');
}

export const options = {
  scenarios: {
    reception_flow: {
      executor: 'per-vu-iterations',
      vus: profiles[profile].vus,
      iterations: profiles[profile].iterations,
      maxDuration: '2m',
      gracefulStop: '20s'
    }
  },
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{name:appointment_schedule}': ['p(95)<750'],
    'http_req_duration{name:appointment_confirm}': ['p(95)<750'],
    'http_req_duration{name:appointment_cancel}': ['p(95)<750'],
    checks: ['rate>0.99']
  }
};

function authorizedPost(path, token, payload, name) {
  return http.post(`${baseUrl}${path}`, JSON.stringify(payload), {
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    tags: { name }
  });
}

export function setup() {
  let token = accessToken;
  if (!token) {
    const login = http.post(`${baseUrl}/api/auth/login`, JSON.stringify({ email: userEmail, password: userPassword }), {
      headers: { 'Content-Type': 'application/json' }, tags: { name: 'auth_login_setup' }
    });
    check(login, { 'login de preparação retorna token': (response) => response.status === 200 && Boolean(response.json('accessToken')) });
    token = login.json('accessToken');
    if (!token) throw new Error(`Login de preparação falhou: HTTP ${login.status}.`);
  }

  const headers = { Authorization: `Bearer ${token}` };
  const doctors = http.get(`${baseUrl}/api/users/doctors`, { headers, tags: { name: 'doctors_setup' } });
  check(doctors, { 'há médico para cenário de carga': (response) => response.status === 200 && Array.isArray(response.json()) && response.json().length > 0 });
  const doctor = doctors.json()[0];
  if (!doctor?.id) throw new Error('Nenhum médico disponível para o cenário de carga.');

  const patient = authorizedPost('/api/patients', token, {
    name: 'Performance Test Patient',
    birthDate: '1990-01-15',
    email: `performance-${runId}@example.test`,
    phone: '+5511999999999'
  }, 'patient_create_setup');
  check(patient, { 'paciente sintético criado': (response) => response.status === 201 && Boolean(response.json('id')) });
  const patientId = patient.json('id');
  if (!patientId) throw new Error(`Paciente sintético não foi criado: HTTP ${patient.status}.`);

  return { token, doctorId: doctor.id, patientId };
}

export default function (session) {
  const read = http.get(`${baseUrl}/api/patients?page=1&pageSize=20`, {
    headers: { Authorization: `Bearer ${session.token}` }, tags: { name: 'patients_search_mixed' }
  });
  check(read, { 'leitura da recepção retorna 200': (response) => response.status === 200 });

  const sequence = (__VU * 100000) + __ITER;
  const startUtc = new Date(Date.now() + (30 * 24 * 60 * 60 * 1000) + (sequence * 31 * 60 * 1000)).toISOString();
  const scheduled = authorizedPost('/api/appointments', session.token, {
    patientId: session.patientId,
    doctorId: session.doctorId,
    startUtc,
    durationMinutes: 30
  }, 'appointment_schedule');
  check(scheduled, { 'consulta sintética agendada': (response) => response.status === 201 && Boolean(response.json('id')) });
  const appointmentId = scheduled.json('id');
  if (!appointmentId) return;

  const confirmed = authorizedPost(`/api/appointments/${appointmentId}/confirm`, session.token, {}, 'appointment_confirm');
  check(confirmed, { 'consulta sintética confirmada': (response) => response.status === 200 });

  const cancelled = authorizedPost(`/api/appointments/${appointmentId}/cancel`, session.token, { reason: 'Performance test cleanup' }, 'appointment_cancel');
  check(cancelled, { 'consulta sintética cancelada': (response) => response.status === 200 });

  sleep(3);
}
