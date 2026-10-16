'use client';

import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import {
  Activity,
  AlertTriangle,
  BookOpen,
  Check,
  CheckCircle2,
  Cpu,
  CreditCard,
  Boxes,
  Loader2,
  Package,
  RadioTower,
  RefreshCw,
  Smartphone,
  X
} from 'lucide-react';

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5050';
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
  const payload = await response.json().catch(() => null);
  if (!response.ok || payload?.success === false) {
    throw new Error(payload?.message || 'The service could not complete this request.');
  }
  return payload?.data ?? payload;
}

const wait = (milliseconds) => new Promise((resolve) => setTimeout(resolve, milliseconds));

function getVendingUnavailableReason(health) {
  if (!health?.state) return 'Machine health is unavailable';
  if (health.state === 'OFFLINE') return 'Machine is offline';
  if (health.doorStatus !== 'CLOSED') return 'Machine door is not closed';
  if (!health.dispenserStatus || ['ERROR', 'UNKNOWN'].includes(health.dispenserStatus)) {
    return 'Dispenser is unavailable';
  }
  if (!['ONLINE', 'DEGRADED'].includes(health.state)) return `Machine state is ${health.state}`;
  return '';
}

export default function DashboardPage() {
  const [machines, setMachines] = useState([]);
  const [health, setHealth] = useState([]);
  const [products, setProducts] = useState([]);
  const [inventory, setInventory] = useState({});
  const [transactions, setTransactions] = useState([]);
  const [payments, setPayments] = useState([]);
  const [selected, setSelected] = useState('VM-001');
  const [purchase, setPurchase] = useState(null);

  async function load() {
    const [machineData, healthData, productData, transactionData, paymentData] = await Promise.all([
      execute('/api/machines', 'getMachines').catch(() => []),
      readJson('/api/telemetry/machines').catch(() => []),
      execute('/api/inventory', 'getProducts').catch(() => []),
      execute('/api/inventory', 'getRecentTransactions').catch(() => []),
      execute('/api/payments', 'getRecentPayments').catch(() => [])
    ]);

    const fleet = machineData.length ? machineData : FALLBACK_MACHINES;
    const inventoryByMachine = {};
    await Promise.all(
      fleet.map(async (machine) => {
        inventoryByMachine[machine.machineCode] = await execute(
          '/api/inventory',
          'getMachineInventory',
          { machineId: machine.machineCode }
        ).catch(() => []);
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

  function openPurchase(machineId) {
    const machineHealth = healthByMachine[machineId];
    const unavailableReason = getVendingUnavailableReason(machineHealth);
    if (unavailableReason) return;
    const slot = (inventory[machineId] || []).find((item) => item.productId === mango?.id);
    if (!mango || !slot) return;
    setPurchase({
      machineId,
      productId: mango.id,
      productName: mango.name,
      available: slot.quantity,
      price: mango.price,
      currency: mango.currency,
      quantity: 1,
      paymentMethod: 'MPESA',
      customerPhone: '254712345678',
      stage: 'FORM',
      transactionId: null,
      error: ''
    });
  }

  async function submitPurchase(event) {
    event.preventDefault();
    const order = purchase;
    if (!order) return;

    try {
      setPurchase((current) => ({ ...current, stage: 'SUBMITTING', error: '' }));
      const transaction = await execute('/api/inventory', 'reserveProduct', {
        machineId: order.machineId,
        productId: order.productId,
        quantity: Number(order.quantity),
        paymentMethod: order.paymentMethod,
        customerPhone: order.paymentMethod === 'MPESA' ? order.customerPhone : ''
      });

      setPurchase((current) => ({
        ...current,
        transactionId: transaction.transactionId,
        stage: order.paymentMethod === 'MPESA' ? 'STK_SENT' : 'PAYMENT_PROCESSING'
      }));
      await followPurchase(transaction.transactionId);
    } catch (error) {
      setPurchase((current) => ({ ...current, stage: 'FORM', error: error.message }));
    }
  }

  async function followPurchase(transactionId) {
    let showedPaymentReceived = false;
    const updatePurchase = (updates) => setPurchase((current) =>
      current?.transactionId === transactionId ? { ...current, ...updates } : current
    );

    for (let attempt = 0; attempt < 30; attempt += 1) {
      await wait(700);
      const latest = await execute('/api/inventory', 'getRecentTransactions').catch(() => []);
      const transaction = latest.find((item) => item.transactionId === transactionId);
      if (!transaction) continue;

      if (['PAYMENT_CONFIRMED', 'DISPENSING', 'COMPLETED'].includes(transaction.status) && !showedPaymentReceived) {
        showedPaymentReceived = true;
        updatePurchase({ stage: 'PAYMENT_RECEIVED' });
        await wait(800);
      }

      if (transaction.status === 'DISPENSING') {
        updatePurchase({ stage: 'DISPENSING' });
      }

      if (transaction.status === 'COMPLETED') {
        updatePurchase({ stage: 'COMPLETED' });
        await load();
        return;
      }

      if (['FAILED', 'DISPENSE_FAILED', 'REFUND_REQUIRED', 'REFUNDED'].includes(transaction.status)) {
        updatePurchase({
          stage: 'FAILED',
          error: transaction.status === 'REFUNDED'
            ? 'Dispensing failed and the payment was refunded.'
            : 'The purchase could not be completed.'
        });
        await load();
        return;
      }
    }

    updatePurchase({
      stage: 'FAILED',
      error: 'The purchase is taking longer than expected. Check Recent Transactions for its status.'
    });
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
        <div className="header-actions">
          <nav aria-label="Primary navigation">
            <span className="nav-link active">Dashboard</span>
            <Link className="nav-link" href="/machines"><Cpu size={15} /> Machines</Link>
            <Link className="nav-link" href="/inventory"><Boxes size={15} /> Inventory</Link>
            <Link className="nav-link" href="/payments"><CreditCard size={15} /> Payments</Link>
            <Link className="nav-link" href="/ledger"><BookOpen size={15} /> Item ledger</Link>
          </nav>
          <button onClick={load}>
            <RefreshCw size={16} />
            Refresh
          </button>
        </div>
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
                const mangoSlot = (inventory[machine.machineCode] || []).find((item) => item.productId === mango?.id);
                const unavailableReason = getVendingUnavailableReason(h);
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
                    <td><div className="action-group">
                      <Link
                        className="table-link"
                        href={`/machines/${machine.machineCode}`}
                        onClick={(event) => event.stopPropagation()}
                      >Telemetry</Link>
                      <button
                        disabled={!mangoSlot || mangoSlot.quantity < 1 || Boolean(unavailableReason)}
                        title={unavailableReason || ''}
                        onClick={(event) => {
                          event.stopPropagation();
                          openPurchase(machine.machineCode);
                        }}
                      >
                        {!mangoSlot || mangoSlot.quantity < 1 ? 'Sold out' : unavailableReason ? 'Unavailable' : 'Buy Mango'}
                      </button>
                    </div></td>
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
              <span>{transaction.quantity || 1} × {transaction.status}</span>
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
            <div className="row product-row" key={product.id}>
              <span>{product.name}</span>
              <b>
                {product.price} {product.currency}
              </b>
            </div>
          ))}
        </Panel>
      </section>

      {purchase && (
        <PurchaseModal
          purchase={purchase}
          setPurchase={setPurchase}
          onSubmit={submitPurchase}
          onClose={() => setPurchase(null)}
        />
      )}
    </main>
  );
}

function PurchaseModal({ purchase, setPurchase, onSubmit, onClose }) {
  const total = Number(purchase.price || 0) * Number(purchase.quantity || 0);
  const isForm = purchase.stage === 'FORM' || purchase.stage === 'SUBMITTING';
  const isBusy = purchase.stage === 'SUBMITTING';
  const stageIndex = {
    STK_SENT: 0,
    PAYMENT_PROCESSING: 0,
    PAYMENT_RECEIVED: 1,
    DISPENSING: 2,
    COMPLETED: 3
  }[purchase.stage] ?? 0;

  return (
    <div className="modal-backdrop" role="presentation">
      <section className="purchase-modal" role="dialog" aria-modal="true" aria-labelledby="purchase-title">
        <button className="icon-button modal-close" onClick={onClose} aria-label="Close purchase dialog">
          <X size={18} />
        </button>

        {isForm ? (
          <form onSubmit={onSubmit}>
            <div className="modal-heading">
              <div className="modal-icon"><Package size={22} /></div>
              <div>
                <span className="eyebrow">{purchase.machineId}</span>
                <h2 id="purchase-title">Buy {purchase.productName}</h2>
                <p>{purchase.available} bottles currently available</p>
              </div>
            </div>

            <div className="form-grid">
              <label>
                Quantity
                <input
                  type="number"
                  min="1"
                  max={purchase.available}
                  value={purchase.quantity}
                  onChange={(event) => setPurchase((current) => ({
                    ...current,
                    quantity: Math.max(1, Math.min(current.available, Number(event.target.value)))
                  }))}
                  required
                />
              </label>
              <label>
                Payment method
                <select
                  value={purchase.paymentMethod}
                  onChange={(event) => setPurchase((current) => ({
                    ...current,
                    paymentMethod: event.target.value
                  }))}
                >
                  <option value="MPESA">M-Pesa STK Push</option>
                  <option value="CARD">Demo card payment</option>
                </select>
              </label>
            </div>

            {purchase.paymentMethod === 'MPESA' && (
              <label>
                M-Pesa phone number
                <input
                  type="tel"
                  inputMode="numeric"
                  pattern="(?:254|0)[0-9]{9}"
                  value={purchase.customerPhone}
                  onChange={(event) => setPurchase((current) => ({
                    ...current,
                    customerPhone: event.target.value.replace(/[^0-9]/g, '')
                  }))}
                  placeholder="254712345678"
                  required
                />
              </label>
            )}

            {purchase.paymentMethod === 'CARD' && (
              <div className="demo-payment-note">
                <CreditCard size={18} /> Demo card approval will be simulated—no card details are required.
              </div>
            )}

            {purchase.error && <div className="form-error">{purchase.error}</div>}

            <div className="order-total">
              <span>Total</span>
              <strong>{purchase.currency} {total.toLocaleString()}</strong>
            </div>

            <button className="primary-button full-button" type="submit" disabled={isBusy || purchase.available < 1}>
              {isBusy ? <Loader2 className="spin" size={17} /> : <Smartphone size={17} />}
              {isBusy ? 'Creating purchase…' : purchase.paymentMethod === 'MPESA' ? 'Send STK push' : 'Pay now'}
            </button>
          </form>
        ) : (
          <div className="payment-progress">
            {purchase.stage === 'COMPLETED' ? (
              <div className="success-mark"><CheckCircle2 size={34} /></div>
            ) : purchase.stage === 'FAILED' ? (
              <div className="failure-mark"><AlertTriangle size={30} /></div>
            ) : (
              <div className="phone-pulse"><Smartphone size={30} /></div>
            )}

            <span className="eyebrow">{purchase.transactionId}</span>
            <h2 id="purchase-title">
              {purchase.stage === 'STK_SENT' && 'Check your phone'}
              {purchase.stage === 'PAYMENT_PROCESSING' && 'Processing payment'}
              {purchase.stage === 'PAYMENT_RECEIVED' && 'Payment received'}
              {purchase.stage === 'DISPENSING' && 'Dispensing your order'}
              {purchase.stage === 'COMPLETED' && 'Purchase complete'}
              {purchase.stage === 'FAILED' && 'Purchase needs attention'}
            </h2>
            <p>
              {purchase.stage === 'STK_SENT' && `A dummy M-Pesa STK push was sent to ${purchase.customerPhone}.`}
              {purchase.stage === 'PAYMENT_PROCESSING' && 'Your demo payment is being authorized.'}
              {purchase.stage === 'PAYMENT_RECEIVED' && 'The payment was confirmed. Preparing the dispenser.'}
              {purchase.stage === 'DISPENSING' && `${purchase.quantity} × ${purchase.productName} is being dispensed.`}
              {purchase.stage === 'COMPLETED' && `Stock was reduced by ${purchase.quantity} and the ledger was updated.`}
              {purchase.stage === 'FAILED' && purchase.error}
            </p>

            {purchase.stage !== 'FAILED' && (
              <div className="purchase-steps">
                {['Payment received', 'Dispensing', 'Stock & ledger updated'].map((label, index) => (
                  <div className={stageIndex > index ? 'step done' : stageIndex === index + 1 ? 'step current' : 'step'} key={label}>
                    <span>{stageIndex > index ? <Check size={14} /> : index + 1}</span>
                    {label}
                  </div>
                ))}
              </div>
            )}

            {purchase.stage === 'COMPLETED' && (
              <div className="modal-actions">
                <button onClick={onClose}>Close</button>
                <Link className="primary-link" href="/ledger"><BookOpen size={16} /> View ledger</Link>
              </div>
            )}
            {purchase.stage === 'FAILED' && <button className="full-button" onClick={onClose}>Close</button>}
          </div>
        )}
      </section>
    </div>
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
