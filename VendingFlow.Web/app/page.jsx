'use client';

import { useEffect, useMemo, useState } from 'react';
import { Activity, AlertTriangle, CreditCard, Package, RadioTower, RefreshCw } from 'lucide-react';

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
const FALLBACK_MACHINES = ['VM-001', 'VM-002', 'VM-003', 'VM-004'].map((machineCode) => ({
  machineCode,
  location: 'Demo location'
}));

async function readJson(path) {
  const response = await fetch(`${API}${path}`, { cache: 'no-store' });
  if (!response.ok) return [];
  return response.json();
}

async function execute(path, service, data = {}) {
  const response = await fetch(`${API}${path}`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ service, data })
  });
  if (!response.ok) return [];
  const payload = await response.json();
  return payload?.data ?? payload;
}

export default function DashboardPage() {
  const [machines, setMachines] = useState([]);
  const [health, setHealth] = useState([]);
  const [products, setProducts] = useState([]);
  const [inventory, setInventory] = useState({});
  const [transactions, setTransactions] = useState([]);
  const [payments, setPayments] = useState([]);
  const [selected, setSelected] = useState('VM-001');

  async function load() {
    const [machineData, healthData, productData, transactionData, paymentData] = await Promise.all([
      execute('/api/machines', 'getMachines'),
      readJson('/api/telemetry/machines'),
      execute('/api/inventory', 'getProducts'),
      execute('/api/inventory', 'getRecentTransactions'),
      execute('/api/payments', 'getRecentPayments')
    ]);

    const fleet = machineData.length ? machineData : FALLBACK_MACHINES;
    const inventoryByMachine = {};
    await Promise.all(
      fleet.map(async (machine) => {
        inventoryByMachine[machine.machineCode] = await execute('/api/inventory', 'getMachineInventory', {
          machineId: machine.machineCode
        });
      })
    );

    setMachines(machineData);
    setHealth(healthData);
    setProducts(productData);
    setTransactions(transactionData);
    setPayments(paymentData);
    setInventory(inventoryByMachine);
  }

  useEffect(() => {
    load();
    const id = setInterval(load, 5000);
    return () => clearInterval(id);
  }, []);

  const mango = products.find((product) => product.name === 'Mango Juice');
  const stats = useMemo(() => {
    const online = health.filter((item) => item.state === 'ONLINE').length;
    const degraded = health.filter((item) => item.state === 'DEGRADED').length;
    const offline = health.filter((item) => item.state === 'OFFLINE').length;
    const revenue = payments
      .filter((payment) => payment.status === 'CONFIRMED')
      .reduce((sum, payment) => sum + Number(payment.amount || 0), 0);
    return { online, degraded, offline, revenue };
  }, [health, payments]);

  async function buy(machineId, productId = mango?.id) {
    if (!productId) return;
    await execute('/api/inventory', 'reserveProduct', { machineId, productId });
    setTimeout(load, 1500);
  }

  const selectedInventory = inventory[selected] || [];
  const healthByMachine = Object.fromEntries(health.map((item) => [item.machineId, item]));
  const fleet = machines.length ? machines : FALLBACK_MACHINES;

  return (
    <main>
      <header>
        <div>
          <h1>VendingFlow</h1>
          <p>Distributed vending fleet operations</p>
        </div>
        <button onClick={load}>
          <RefreshCw size={16} />
          Refresh
        </button>
      </header>

      <section className="stats">
        <Card icon={<RadioTower />} label="Total Machines" value={fleet.length} />
        <Card icon={<Activity />} label="Online" value={stats.online} />
        <Card icon={<AlertTriangle />} label="Degraded" value={stats.degraded} />
        <Card icon={<AlertTriangle />} label="Offline" value={stats.offline} />
        <Card icon={<CreditCard />} label="Today's Transactions" value={transactions.length} />
        <Card icon={<CreditCard />} label="Today's Revenue" value={`KES ${stats.revenue}`} />
      </section>

      <section className="grid">
        <div className="panel wide">
          <h2>Machines</h2>
          <table>
            <thead>
              <tr>
                <th>Machine</th>
                <th>Location</th>
                <th>Status</th>
                <th>Last Heartbeat</th>
                <th>Stock</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {fleet.map((machine) => {
                const h = healthByMachine[machine.machineCode] || {};
                const stock = (inventory[machine.machineCode] || []).reduce((sum, item) => sum + item.quantity, 0);
                return (
                  <tr
                    key={machine.machineCode}
                    onClick={() => setSelected(machine.machineCode)}
                    className={selected === machine.machineCode ? 'selected' : ''}
                  >
                    <td>{machine.machineCode}</td>
                    <td>{machine.location}</td>
                    <td>
                      <span className={`pill ${h.state || machine.status || 'UNKNOWN'}`}>
                        {h.state || machine.status || 'UNKNOWN'}
                      </span>
                    </td>
                    <td>{h.lastHeartbeatAt ? new Date(h.lastHeartbeatAt).toLocaleTimeString() : '-'}</td>
                    <td>{stock}</td>
                    <td>
                      <button
                        onClick={(event) => {
                          event.stopPropagation();
                          buy(machine.machineCode);
                        }}
                      >
                        Buy Mango
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        <Panel title={`${selected} Inventory`}>
          {selectedInventory.slice(0, 8).map((item) => (
            <div className="row" key={item.slotNumber}>
              <Package size={16} />
              <span>{item.productName}</span>
              <b>
                {item.quantity}/{item.capacity}
              </b>
            </div>
          ))}
        </Panel>

        <Panel title="Recent Transactions">
          {transactions.slice(0, 8).map((transaction) => (
            <div className="row" key={transaction.transactionId}>
              <span>{transaction.machineId}</span>
              <span>{transaction.status}</span>
              <b>
                {transaction.amount} {transaction.currency}
              </b>
            </div>
          ))}
        </Panel>

        <Panel title="Active Alerts">
          {health
            .filter((item) => item.state !== 'ONLINE')
            .map((item) => (
              <div className="alert" key={item.machineId}>
                {item.machineId} {item.state} {item.dispenserStatus === 'ERROR' ? 'dispenser error' : ''}
              </div>
            ))}
          {Object.values(inventory)
            .flat()
            .filter((item) => item.quantity <= item.lowStockThreshold)
            .slice(0, 6)
            .map((item) => (
              <div className="alert" key={`${item.machineId}-${item.slotNumber}`}>
                {item.machineId} low {item.productName}
              </div>
            ))}
        </Panel>

        <Panel title="Top Products">
          {products.slice(0, 6).map((product) => (
            <div className="row" key={product.id}>
              <span>{product.name}</span>
              <b>
                {product.price} {product.currency}
              </b>
            </div>
          ))}
        </Panel>
      </section>
    </main>
  );
}

function Card({ icon, label, value }) {
  return (
    <div className="card">
      {icon}
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function Panel({ title, children }) {
  return (
    <div className="panel">
      <h2>{title}</h2>
      {children}
    </div>
  );
}
