'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import { Banknote, BookOpen, Boxes, CheckCircle2, Clock3, Cpu, CreditCard, LayoutDashboard, RefreshCw, Smartphone } from 'lucide-react';

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5050';

async function getPayments() {
  const response = await fetch(`${API}/api/payments`, {
    method: 'POST', headers: { 'content-type': 'application/json' }, cache: 'no-store',
    body: JSON.stringify({ service: 'getRecentPayments', data: {} })
  });
  if (!response.ok) throw new Error('Unable to load payments.');
  const payload = await response.json();
  if (payload?.success === false) throw new Error(payload.message || 'Unable to load payments.');
  return payload?.data ?? [];
}

const displayMethod = (method) => method === 'MPESA' ? 'M-Pesa' : method || 'Legacy';

export default function PaymentsPage() {
  const [payments, setPayments] = useState([]);
  const [machineId, setMachineId] = useState('');
  const [status, setStatus] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try { setPayments(await getPayments()); }
    catch (requestError) { setError(requestError.message); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { load(); }, [load]);

  const machines = useMemo(() => [...new Set(payments.map((payment) => payment.machineId).filter(Boolean))].sort(), [payments]);
  const visiblePayments = useMemo(() => payments.filter((payment) =>
    (!machineId || payment.machineId === machineId) && (!status || payment.status === status)
  ), [payments, machineId, status]);
  const summary = useMemo(() => {
    const confirmed = visiblePayments.filter((payment) => payment.status === 'CONFIRMED');
    return {
      revenue: confirmed.reduce((sum, payment) => sum + Number(payment.amount || 0), 0),
      confirmed: confirmed.length,
      pending: visiblePayments.filter((payment) => ['PENDING', 'PROCESSING'].includes(payment.status)).length,
      mpesa: visiblePayments.filter((payment) => payment.paymentMethod === 'MPESA').length
    };
  }, [visiblePayments]);

  return (
    <main>
      <header>
        <div><h1>Payments</h1><p>Payment activity and settlement status for vending purchases.</p></div>
        <div className="header-actions">
          <nav aria-label="Primary navigation">
            <Link className="nav-link" href="/"><LayoutDashboard size={15} /> Dashboard</Link>
            <Link className="nav-link" href="/machines"><Cpu size={15} /> Machines</Link>
            <Link className="nav-link" href="/inventory"><Boxes size={15} /> Inventory</Link>
            <span className="nav-link active"><CreditCard size={15} /> Payments</span>
            <Link className="nav-link" href="/ledger"><BookOpen size={15} /> Item ledger</Link>
          </nav>
          <button onClick={load}><RefreshCw size={16} /> Refresh</button>
        </div>
      </header>

      <section className="ledger-stats four-up">
        <div className="ledger-stat"><Banknote /><span>Confirmed revenue</span><strong>KES {summary.revenue.toLocaleString()}</strong></div>
        <div className="ledger-stat"><CheckCircle2 /><span>Confirmed payments</span><strong>{summary.confirmed}</strong></div>
        <div className="ledger-stat"><Clock3 /><span>Awaiting completion</span><strong>{summary.pending}</strong></div>
        <div className="ledger-stat"><Smartphone /><span>M-Pesa payments</span><strong>{summary.mpesa}</strong></div>
      </section>

      <section className="panel ledger-panel">
        <div className="panel-title-row page-toolbar">
          <div><h2>Payment history</h2><p>Newest payments appear first.</p></div>
          <div className="table-filters">
            <select aria-label="Filter by machine" value={machineId} onChange={(event) => setMachineId(event.target.value)}><option value="">All machines</option>{machines.map((machine) => <option key={machine}>{machine}</option>)}</select>
            <select aria-label="Filter by status" value={status} onChange={(event) => setStatus(event.target.value)}><option value="">All statuses</option><option>CONFIRMED</option><option>PROCESSING</option><option>PENDING</option><option>FAILED</option><option>REFUNDED</option></select>
            {!loading && <span className="record-count">{visiblePayments.length} payments</span>}
          </div>
        </div>
        {error && <div className="form-error">{error}</div>}
        {loading ? <div className="empty-state">Loading payments…</div> : visiblePayments.length === 0 ? (
          <div className="empty-state"><CreditCard size={28} /><strong>No payments found</strong><span>Complete a purchase or change the current filters.</span></div>
        ) : (
          <div className="table-scroll"><table className="ledger-table"><thead><tr><th>Date</th><th>Payment reference</th><th>Vend reference</th><th>Machine</th><th>Method</th><th>Customer</th><th>Amount</th><th>Status</th></tr></thead>
            <tbody>{visiblePayments.map((payment) => <tr key={payment.paymentTransactionId}>
              <td>{payment.createdAt ? new Date(payment.createdAt).toLocaleString() : '—'}</td>
              <td><code>{payment.paymentTransactionCode}</code></td><td><code>{payment.vendingTransactionId}</code></td><td><strong>{payment.machineId}</strong></td>
              <td>{displayMethod(payment.paymentMethod)}</td><td>{payment.customerPhone || '—'}</td><td><strong>{payment.currency} {Number(payment.amount).toLocaleString()}</strong></td>
              <td><span className={`state-pill payment-${payment.status.toLowerCase()}`}>{payment.status}</span></td>
            </tr>)}</tbody></table></div>
        )}
      </section>
    </main>
  );
}
