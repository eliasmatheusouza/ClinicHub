export interface Patient { id: string; name: string; birthDate: string; email: string; phone: string; isActive: boolean; }
export interface PagedResult<T> { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number; }
export interface DoctorOption { id: string; email: string; }
export interface Appointment { id: string; patientId: string; doctorId: string; startUtc: string; endUtc: string; durationMinutes: number; status: string; cancellationReason?: string; }
export interface Payment { id: string; appointmentId: string; amount: number; currency: string; method: string; paidAtUtc: string; }
export interface DailyRevenue { date: string; currency: string; grossRevenue: number; paymentCount: number; }
export interface RevenueReport { startDate: string; endDate: string; items: DailyRevenue[]; }
