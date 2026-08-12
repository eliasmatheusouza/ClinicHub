import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = (__ENV.BASE_URL || 'http://host.docker.internal:8082').replace(/\/$/, '');
const profile = __ENV.PERF_PROFILE || 'smoke';
const userEmail = __ENV.PERF_USER_EMAIL;
const userPassword = __ENV.PERF_USER_PASSWORD;
const thinkTimeSeconds = Number(__ENV.PERF_THINK_TIME_SECONDS || '1');

const profiles = {
  smoke: [
    { duration: '10s', target: 2 },
    { duration: '20s', target: 5 },
    { duration: '10s', target: 0 }
  ],
  baseline: [
    { duration: '30s', target: 10 },
    { duration: '30s', target: 25 },
    { duration: '1m', target: 25 },
    { duration: '30s', target: 0 }
  ]
};

if (!profiles[profile]) {
  throw new Error(`PERF_PROFILE inválido: "${profile}". Use "smoke" ou "baseline".`);
}

if (!userEmail || !userPassword) {
  throw new Error('Defina PERF_USER_EMAIL e PERF_USER_PASSWORD antes de executar o teste.');
}

export const options = {
  stages: profiles[profile],
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    'http_req_duration{name:patients_search}': ['p(95)<400'],
    checks: ['rate>0.99']
  }
};

export function setup() {
  const response = http.post(
    `${baseUrl}/api/auth/login`,
    JSON.stringify({ email: userEmail, password: userPassword }),
    {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'auth_login_setup' }
    });

  const authenticated = check(response, {
    'login de preparação retorna 200': (result) => result.status === 200,
    'login de preparação retorna access token': (result) => Boolean(result.json('accessToken'))
  });

  if (!authenticated) {
    throw new Error(`Não foi possível autenticar o usuário de carga. Status: ${response.status}.`);
  }

  return { accessToken: response.json('accessToken') };
}

export default function (session) {
  const response = http.get(`${baseUrl}/api/patients?page=1&pageSize=20`, {
    headers: { Authorization: `Bearer ${session.accessToken}` },
    tags: { name: 'patients_search' }
  });

  check(response, {
    'listagem de pacientes retorna 200': (result) => result.status === 200,
    'listagem de pacientes retorna dados paginados': (result) => Array.isArray(result.json('items'))
  });

  sleep(thinkTimeSeconds);
}
