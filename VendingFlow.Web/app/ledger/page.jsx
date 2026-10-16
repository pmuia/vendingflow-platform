'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import { ArrowLeft, BookOpen, Boxes, Cpu, CreditCard, LayoutDashboard, RefreshCw, TrendingDown, WalletCards } from 'lucide-react';

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5050';

async function getLedger(machineId = '') {
  const response = await fetch(`${API}/api/inventory`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    cache: 'no-store',
    body: JSON.stringify({
      service: 'getItemLedger',
      data: { machineId: machineId || null }
    })
  });
  if (!response.ok) throw new Error('Unable to load the item ledger.');
  const payload = await response.json();
  if (payload?.success === false) throw new Error(payload.message || 'Unable to load the item ledger.');
  return payload?.data ?? [];
}

export default function ItemLedgerPage() {
  const [entries, setEntries] = useState([]);
  const [machineId, setMachineId] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      setEntries(await getLedger(machineId));
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setLoading(false);
    }
  }, [machineId]);

  useEffect(() => {
    load();
  }, [load]);

  const summary = useMemo(() => ({
    movements: entries.length,
    unitsSold: entries
      .filter((entry) => entry.movementType === 'SALE')
      .reduce((sum, entry) => sum + Math.abs(entry.quantityChange), 0),
    salesValue: entries
      .filter((entry) => entry.movementType === 'SALE')
      .reduce((sum, entry) => sum + Number(entry.totalAmount || 0), 0)
  }), [entries]);

  return (
    <main>
      <header>
        <div>
          <Link className="back-link" href="/"><ArrowLeft size={15} /> Dashboard</Link>
          <h1>Item ledger</h1>
          <p>Every stock movement, purchase, and resulting machine balance.</p>
        </div>
        <div className="header-actions">
          <nav aria-label="Primary navigation">
            <Link className="nav-link" href="/"><LayoutDashboard size={15} /> Dashboard</Link>
            <Link className="nav-link" href="/machines"><Cpu size={15} /> Machines</Link>
            <Link className="nav-link" href="/inventory"><Boxes size={15} /> Inventory</Link>
            <Link className="nav-link" href="/payments"><CreditCard size={15} /> Payments</Link>
            <span className="nav-link active"><BookOpen size={15} /> Item ledger</span>
          </nav>
          <label className="machine-filter">
            Machine
            <select value={machineId} onChange={(event) => setMachineId(event.target.value)}>
              <option value="">All machines</option>
              <option value="VM-001">VM-001</option>
              <option value="VM-002">VM-002</option>
              <option value="VM-003">VM-003</option>
              <option value="VM-004">VM-004</option>
            </select>
          </label>
          <button onClick={load}><RefreshCw size={16} /> Refresh</button>
        </div>
      </header>

      <section className="ledger-stats">
        <div className="ledger-stat"><BookOpen /><span>Movements</span><strong>{summary.movements}</strong></div>
        <div className="ledger-stat"><TrendingDown /><span>Units sold</span><strong>{summary.unitsSold}</strong></div>
        <div className="ledger-stat"><WalletCards /><span>Sales value</span><strong>KES {summary.salesValue.toLocaleString()}</strong></div>
      </section>

      <section className="panel ledger-panel">
        <div className="panel-title-row">
          <div>
            <h2>Movement history</h2>
            <p>Newest entries appear first.</p>
          </div>
          {!loading && <span className="record-count">{entries.length} records</span>}
        </div>

        {error && <div className="form-error">{error}</div>}
        {loading ? (
          <div className="empty-state">Loading ledger entries…</div>
        ) : entries.length === 0 ? (
          <div className="empty-state">
            <BookOpen size={28} />
            <strong>No ledger entries yet</strong>
            <span>Complete a purchase from the dashboard to create the first sale entry.</span>
          </div>
        ) : (
          <div className="table-scroll">
            <table className="ledger-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Machine</th>
                  <th>Product</th>
                  <th>Movement</th>
                  <th>Quantity</th>
                  <th>Balance</th>
                  <th>Amount</th>
                  <th>Reference</th>
                </tr>
              </thead>
              <tbody>
                {entries.map((entry) => (
                  <tr key={entry.id}>
                    <td>{new Date(entry.createdAt).toLocaleString()}</td>
                    <td><strong>{entry.machineId}</strong></td>
                    <td>{entry.productName}</td>
                    <td><span className={`movement ${entry.movementType}`}>{entry.movementType.replace('_', ' ')}</span></td>
                    <td className={entry.quantityChange < 0 ? 'quantity-out' : 'quantity-in'}>
                      {entry.quantityChange > 0 ? '+' : ''}{entry.quantityChange}
                    </td>
                    <td>{entry.balanceAfter}</td>
                    <td>{entry.currency} {Number(entry.totalAmount).toLocaleString()}</td>
                    <td><code>{entry.transactionId}</code></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}
