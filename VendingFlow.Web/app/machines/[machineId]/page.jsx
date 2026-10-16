'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { Activity, ArrowLeft, BookOpen, Boxes, Cpu, CreditCard, DoorClosed, Gauge, LayoutDashboard, RefreshCw, Signal, Thermometer, Wrench } from 'lucide-react';

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5050';

async function read(path) {
  const response = await fetch(`${API}${path}`, { cache: 'no-store' });
  if (!response.ok) throw new Error('Unable to load telemetry.');
  return response.json();
}

async function execute(path, service, data = {}) {
  const response = await fetch(`${API}${path}`, {
    method: 'POST', headers: { 'content-type': 'application/json' }, cache: 'no-store',
    body: JSON.stringify({ service, data })
  });
  if (!response.ok) throw new Error('Unable to load machine details.');
  const payload = await response.json();
  if (payload?.success === false) throw new Error(payload.message || 'Unable to load machine details.');
  return payload?.data ?? payload;
}

const Status = ({ value }) => <span className={`state-pill telemetry-${String(value || 'unknown').toLowerCase()}`}>{value || 'UNKNOWN'}</span>;

export default function MachineTelemetryPage() {
  const { machineId } = useParams();
  const [machine, setMachine] = useState(null);
  const [current, setCurrent] = useState(null);
  const [history, setHistory] = useState([]);
  const [inventory, setInventory] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    if (!machineId) return;
    setLoading(true); setError('');
    try {
      const [machines, currentData, historyData, inventoryData] = await Promise.all([
        execute('/api/machines', 'getMachines'),
        read(`/api/telemetry/machines/${machineId}`),
        read(`/api/telemetry/machines/${machineId}/history`),
        execute('/api/inventory', 'getMachineInventory', { machineId })
      ]);
      setMachine(machines.find((item) => item.machineCode === machineId) || null);
      setCurrent(currentData); setHistory(historyData); setInventory(inventoryData);
    } catch (requestError) { setError(requestError.message); }
    finally { setLoading(false); }
  }, [machineId]);

  useEffect(() => {
    load();
    const interval = setInterval(load, 10000);
    return () => clearInterval(interval);
  }, [load]);

  const latest = history[0] || {};
  const summary = useMemo(() => {
    const temperatures = history.map((item) => Number(item.temperature)).filter(Number.isFinite);
    return {
      averageTemperature: temperatures.length ? (temperatures.reduce((sum, value) => sum + value, 0) / temperatures.length).toFixed(1) : '—',
      minTemperature: temperatures.length ? Math.min(...temperatures).toFixed(1) : '—',
      maxTemperature: temperatures.length ? Math.max(...temperatures).toFixed(1) : '—',
      faults: history.filter((item) => item.healthState !== 'ONLINE' || item.dispenserStatus === 'ERROR' || item.cashModuleStatus === 'ERROR').length,
      units: inventory.reduce((sum, item) => sum + item.quantity, 0)
    };
  }, [history, inventory]);

  return (
    <main>
      <header>
        <div><Link className="back-link" href="/machines"><ArrowLeft size={15} /> All machines</Link><h1>{machineId} telemetry</h1><p>{machine?.name || 'Vending machine'} · {machine?.location || 'Loading location…'}</p></div>
        <div className="header-actions">
          <nav aria-label="Primary navigation"><Link className="nav-link" href="/"><LayoutDashboard size={15} /> Dashboard</Link><Link className="nav-link active" href="/machines"><Cpu size={15} /> Machines</Link><Link className="nav-link" href="/inventory"><Boxes size={15} /> Inventory</Link><Link className="nav-link" href="/payments"><CreditCard size={15} /> Payments</Link><Link className="nav-link" href="/ledger"><BookOpen size={15} /> Item ledger</Link></nav>
          <button onClick={load}><RefreshCw size={16} /> Refresh</button>
        </div>
      </header>

      {error && <div className="form-error">{error}</div>}
      {loading && !current ? <div className="panel empty-state">Loading telemetry…</div> : <>
        <section className="telemetry-hero panel">
          <div><span className="eyebrow">Current health</span><div className="health-heading"><Activity size={28} /><strong>{current?.state || 'UNKNOWN'}</strong></div><p>Last heartbeat {current?.lastHeartbeatAt ? new Date(current.lastHeartbeatAt).toLocaleString() : 'not available'}</p></div>
          <div className="live-indicator"><i /> Live · refreshes every 10 seconds</div>
        </section>

        <section className="telemetry-cards">
          <div className="telemetry-card"><Thermometer /><span>Temperature</span><strong>{latest.temperature ?? '—'}°C</strong><small>Range {summary.minTemperature}–{summary.maxTemperature}°C</small></div>
          <div className="telemetry-card"><Signal /><span>Network strength</span><strong>{current?.networkStrength ?? '—'}%</strong><div className="signal-track"><i style={{ width: `${current?.networkStrength || 0}%` }} /></div></div>
          <div className="telemetry-card"><Wrench /><span>Dispenser</span><strong><Status value={current?.dispenserStatus} /></strong><small>Cash module: {current?.cashModuleStatus || '—'}</small></div>
          <div className="telemetry-card"><DoorClosed /><span>Door</span><strong><Status value={current?.doorStatus} /></strong><small>Expected state: CLOSED</small></div>
          <div className="telemetry-card"><Gauge /><span>Recent fault readings</span><strong>{summary.faults}</strong><small>Across the latest {history.length} heartbeats</small></div>
          <div className="telemetry-card"><Boxes /><span>Current stock</span><strong>{summary.units} units</strong><small>{inventory.filter((item) => item.quantity <= item.lowStockThreshold).length} low-stock slots</small></div>
        </section>

        <section className="telemetry-layout">
          <div className="panel telemetry-history"><div className="panel-title-row"><div><h2>Telemetry history</h2><p>Latest 50 machine heartbeats.</p></div><span className="record-count">Average {summary.averageTemperature}°C</span></div>
            <div className="table-scroll"><table className="ledger-table"><thead><tr><th>Recorded</th><th>Health</th><th>Temperature</th><th>Network</th><th>Dispenser</th><th>Cash module</th><th>Door</th><th>Software</th></tr></thead>
              <tbody>{history.map((item) => <tr key={item.machineTelemetryId}><td>{new Date(item.recordedAt).toLocaleString()}</td><td><Status value={item.healthState} /></td><td>{item.temperature}°C</td><td><div className="network-cell"><span>{item.networkStrength}%</span><div className="signal-track compact"><i style={{ width: `${item.networkStrength}%` }} /></div></div></td><td><Status value={item.dispenserStatus} /></td><td><Status value={item.cashModuleStatus} /></td><td><Status value={item.doorStatus} /></td><td>{item.softwareVersion}</td></tr>)}</tbody>
            </table></div>
          </div>
          <aside className="panel machine-sidebar"><h2>Operational checks</h2>
            <div className="check-row"><span>Heartbeat</span><Status value={current?.state === 'OFFLINE' ? 'MISSING' : 'RECEIVING'} /></div>
            <div className="check-row"><span>Cooling range</span><Status value={Number(latest.temperature) >= 2 && Number(latest.temperature) <= 10 ? 'NORMAL' : 'CHECK'} /></div>
            <div className="check-row"><span>Connectivity</span><Status value={Number(current?.networkStrength) >= 50 ? 'GOOD' : 'WEAK'} /></div>
            <div className="check-row"><span>Dispenser</span><Status value={current?.dispenserStatus} /></div>
            <div className="check-row"><span>Cash module</span><Status value={current?.cashModuleStatus} /></div>
            <div className="check-row"><span>Door security</span><Status value={current?.doorStatus} /></div>
          </aside>
        </section>
      </>}
    </main>
  );
}
