'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import { Activity, AlertTriangle, BookOpen, Boxes, Cpu, CreditCard, ExternalLink, LayoutDashboard, RadioTower, RefreshCw, Server } from 'lucide-react';

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5050';

async function read(path) {
  const response = await fetch(`${API}${path}`, { cache: 'no-store' });
  if (!response.ok) throw new Error('Unable to load machine telemetry.');
  return response.json();
}

async function execute(path, service, data = {}) {
  const response = await fetch(`${API}${path}`, {
    method: 'POST', headers: { 'content-type': 'application/json' }, cache: 'no-store',
    body: JSON.stringify({ service, data })
  });
  if (!response.ok) throw new Error('Unable to load machines.');
  const payload = await response.json();
  if (payload?.success === false) throw new Error(payload.message || 'Unable to load machines.');
  return payload?.data ?? [];
}

const ageText = (date) => {
  if (!date) return 'No heartbeat';
  const seconds = Math.max(0, Math.round((Date.now() - new Date(date).getTime()) / 1000));
  if (seconds < 60) return `${seconds}s ago`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
  return `${Math.floor(seconds / 3600)}h ago`;
};

export default function MachinesPage() {
  const [machines, setMachines] = useState([]);
  const [health, setHealth] = useState([]);
  const [inventory, setInventory] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const [machineData, healthData] = await Promise.all([
        execute('/api/machines', 'getMachines'), read('/api/telemetry/machines')
      ]);
      const stockEntries = await Promise.all(machineData.map(async (machine) => [
        machine.machineCode,
        await execute('/api/inventory', 'getMachineInventory', { machineId: machine.machineCode })
      ]));
      setMachines(machineData); setHealth(healthData); setInventory(Object.fromEntries(stockEntries));
    } catch (requestError) { setError(requestError.message); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => {
    load();
    const interval = setInterval(load, 10000);
    return () => clearInterval(interval);
  }, [load]);

  const healthByMachine = useMemo(() => Object.fromEntries(health.map((item) => [item.machineId, item])), [health]);
  const summary = useMemo(() => ({
    total: machines.length,
    online: health.filter((item) => item.state === 'ONLINE').length,
    attention: health.filter((item) => item.state !== 'ONLINE').length,
    averageNetwork: health.length ? Math.round(health.reduce((sum, item) => sum + Number(item.networkStrength || 0), 0) / health.length) : 0
  }), [machines, health]);

  return (
    <main>
      <header>
        <div><h1>Machines</h1><p>Fleet health, component status, connectivity, and stock at a glance.</p></div>
        <div className="header-actions">
          <nav aria-label="Primary navigation">
            <Link className="nav-link" href="/"><LayoutDashboard size={15} /> Dashboard</Link>
            <span className="nav-link active"><Cpu size={15} /> Machines</span>
            <Link className="nav-link" href="/inventory"><Boxes size={15} /> Inventory</Link>
            <Link className="nav-link" href="/payments"><CreditCard size={15} /> Payments</Link>
            <Link className="nav-link" href="/ledger"><BookOpen size={15} /> Item ledger</Link>
          </nav>
          <button onClick={load}><RefreshCw size={16} /> Refresh</button>
        </div>
      </header>

      <section className="ledger-stats four-up">
        <div className="ledger-stat"><Server /><span>Total machines</span><strong>{summary.total}</strong></div>
        <div className="ledger-stat"><Activity /><span>Online</span><strong>{summary.online}</strong></div>
        <div className="ledger-stat"><AlertTriangle /><span>Need attention</span><strong>{summary.attention}</strong></div>
        <div className="ledger-stat"><RadioTower /><span>Average network</span><strong>{summary.averageNetwork}%</strong></div>
      </section>

      {error && <div className="form-error">{error}</div>}
      {loading && machines.length === 0 ? <div className="panel empty-state">Loading machines…</div> : (
        <section className="machine-card-grid">
          {machines.map((machine) => {
            const telemetry = healthByMachine[machine.machineCode] || {};
            const stock = inventory[machine.machineCode] || [];
            const units = stock.reduce((sum, item) => sum + item.quantity, 0);
            const lowStock = stock.filter((item) => item.quantity <= item.lowStockThreshold).length;
            return <article className={`machine-card ${telemetry.state === 'DEGRADED' || telemetry.state === 'OFFLINE' ? 'needs-attention' : ''}`} key={machine.machineCode}>
              <div className="machine-card-heading"><div><span className="eyebrow">{machine.machineCode}</span><h2>{machine.name}</h2></div><span className={`pill ${telemetry.state || machine.status}`}>{telemetry.state || machine.status}</span></div>
              <p className="machine-location">{machine.location}</p>
              <dl className="machine-metrics">
                <div><dt>Last heartbeat</dt><dd>{ageText(telemetry.lastHeartbeatAt)}</dd></div>
                <div><dt>Network</dt><dd>{telemetry.networkStrength ?? '—'}%</dd></div>
                <div><dt>Dispenser</dt><dd className={telemetry.dispenserStatus === 'ERROR' ? 'metric-error' : ''}>{telemetry.dispenserStatus || '—'}</dd></div>
                <div><dt>Cash module</dt><dd>{telemetry.cashModuleStatus || '—'}</dd></div>
                <div><dt>Door</dt><dd>{telemetry.doorStatus || '—'}</dd></div>
                <div><dt>Stock</dt><dd>{units} units · {lowStock} low</dd></div>
              </dl>
              <Link className="primary-link card-action" href={`/machines/${machine.machineCode}`}>View telemetry <ExternalLink size={14} /></Link>
            </article>;
          })}
        </section>
      )}
    </main>
  );
}
