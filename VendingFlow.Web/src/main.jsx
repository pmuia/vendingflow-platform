import React, { useEffect, useMemo, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Activity, AlertTriangle, CreditCard, Package, RadioTower, RefreshCw } from 'lucide-react';
import './styles.css';

const API = import.meta.env.VITE_API_URL || 'http://localhost:5000';

function App() {
  const [machines, setMachines] = useState([]);
  const [health, setHealth] = useState([]);
  const [products, setProducts] = useState([]);
  const [inventory, setInventory] = useState({});
  const [transactions, setTransactions] = useState([]);
  const [payments, setPayments] = useState([]);
  const [selected, setSelected] = useState('VM-001');

  async function load() {
    const json = async (path) => fetch(`${API}${path}`).then(r => r.ok ? r.json() : []);
    const [m, h, p, t, pay] = await Promise.all([
      json('/api/machines'), json('/api/telemetry/machines'), json('/api/inventory/products'), json('/api/inventory/transactions/recent'), json('/api/payments/recent')
    ]);
    setMachines(m); setHealth(h); setProducts(p); setTransactions(t); setPayments(pay);
    const inv = {};
    await Promise.all((m.length ? m : [{ machineCode: 'VM-001' }, { machineCode: 'VM-002' }, { machineCode: 'VM-003' }, { machineCode: 'VM-004' }]).map(async machine => {
      inv[machine.machineCode] = await json(`/api/inventory/machines/${machine.machineCode}`);
    }));
    setInventory(inv);
  }

  useEffect(() => { load(); const id = setInterval(load, 5000); return () => clearInterval(id); }, []);

  const mango = products.find(p => p.name === 'Mango Juice');
  const stats = useMemo(() => {
    const online = health.filter(h => h.state === 'ONLINE').length;
    const degraded = health.filter(h => h.state === 'DEGRADED').length;
    const offline = health.filter(h => h.state === 'OFFLINE').length;
    const revenue = payments.filter(p => p.status === 'CONFIRMED').reduce((sum, p) => sum + Number(p.amount || 0), 0);
    return { online, degraded, offline, revenue };
  }, [health, payments]);

  async function buy(machineId, productId = mango?.id) {
    if (!productId) return;
    await fetch(`${API}/api/inventory/machines/${machineId}/purchase`, { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ productId }) });
    setTimeout(load, 1500);
  }

  const selectedInventory = inventory[selected] || [];
  const healthByMachine = Object.fromEntries(health.map(h => [h.machineId, h]));

  return <main>
    <header>
      <div><h1>VendingFlow</h1><p>Distributed vending fleet operations</p></div>
      <button onClick={load}><RefreshCw size={16}/>Refresh</button>
    </header>
    <section className="stats">
      <Card icon={<RadioTower/>} label="Total Machines" value={machines.length || 4}/>
      <Card icon={<Activity/>} label="Online" value={stats.online}/>
      <Card icon={<AlertTriangle/>} label="Degraded" value={stats.degraded}/>
      <Card icon={<AlertTriangle/>} label="Offline" value={stats.offline}/>
      <Card icon={<CreditCard/>} label="Today's Transactions" value={transactions.length}/>
      <Card icon={<CreditCard/>} label="Today's Revenue" value={`KES ${stats.revenue}`}/>
    </section>
    <section className="grid">
      <div className="panel wide">
        <h2>Machines</h2>
        <table><thead><tr><th>Machine</th><th>Location</th><th>Status</th><th>Last Heartbeat</th><th>Stock</th><th>Action</th></tr></thead><tbody>
          {(machines.length ? machines : ['VM-001','VM-002','VM-003','VM-004'].map(machineCode => ({machineCode, location:'Demo location'}))).map(m => {
            const h = healthByMachine[m.machineCode] || {};
            const stock = (inventory[m.machineCode] || []).reduce((s, i) => s + i.quantity, 0);
            return <tr key={m.machineCode} onClick={() => setSelected(m.machineCode)} className={selected === m.machineCode ? 'selected' : ''}>
              <td>{m.machineCode}</td><td>{m.location}</td><td><span className={`pill ${h.state || m.status}`}>{h.state || m.status || 'UNKNOWN'}</span></td><td>{h.lastHeartbeatAt ? new Date(h.lastHeartbeatAt).toLocaleTimeString() : '-'}</td><td>{stock}</td><td><button onClick={(e)=>{e.stopPropagation(); buy(m.machineCode)}}>Buy Mango</button></td>
            </tr>
          })}
        </tbody></table>
      </div>
      <div className="panel">
        <h2>{selected} Inventory</h2>
        {selectedInventory.slice(0,8).map(i => <div className="row" key={i.slotNumber}><Package size={16}/><span>{i.productName}</span><b>{i.quantity}/{i.capacity}</b></div>)}
      </div>
      <div className="panel">
        <h2>Recent Transactions</h2>
        {transactions.slice(0,8).map(t => <div className="row" key={t.transactionId}><span>{t.machineId}</span><span>{t.status}</span><b>{t.amount} {t.currency}</b></div>)}
      </div>
      <div className="panel">
        <h2>Active Alerts</h2>
        {health.filter(h => h.state !== 'ONLINE').map(h => <div className="alert" key={h.machineId}>{h.machineId} {h.state} {h.dispenserStatus === 'ERROR' ? 'dispenser error' : ''}</div>)}
        {Object.values(inventory).flat().filter(i => i.quantity <= i.lowStockThreshold).slice(0,6).map(i => <div className="alert" key={`${i.machineId}-${i.slotNumber}`}>{i.machineId} low {i.productName}</div>)}
      </div>
      <div className="panel">
        <h2>Top Products</h2>
        {products.slice(0,6).map(p => <div className="row" key={p.id}><span>{p.name}</span><b>{p.price} {p.currency}</b></div>)}
      </div>
    </section>
  </main>;
}

function Card({icon, label, value}) { return <div className="card">{React.cloneElement(icon,{size:20})}<span>{label}</span><strong>{value}</strong></div>; }

createRoot(document.getElementById('root')).render(<App />);
