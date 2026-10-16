'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import { AlertTriangle, BookOpen, Boxes, Cpu, CreditCard, LayoutDashboard, PackageCheck, RefreshCw, Warehouse } from 'lucide-react';

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5050';
const MACHINES = ['VM-001', 'VM-002', 'VM-003', 'VM-004'];

async function execute(service, data = {}) {
  const response = await fetch(`${API}/api/inventory`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    cache: 'no-store',
    body: JSON.stringify({ service, data })
  });
  if (!response.ok) throw new Error('Unable to load inventory.');
  const payload = await response.json();
  if (payload?.success === false) throw new Error(payload.message || 'Unable to load inventory.');
  return payload?.data ?? [];
}

export default function InventoryPage() {
  const [items, setItems] = useState([]);
  const [products, setProducts] = useState([]);
  const [machineId, setMachineId] = useState('');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [productData, ...machineInventory] = await Promise.all([
        execute('getProducts'),
        ...MACHINES.map((machine) => execute('getMachineInventory', { machineId: machine }))
      ]);
      setProducts(productData);
      setItems(machineInventory.flat());
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const priceByProduct = useMemo(
    () => Object.fromEntries(products.map((product) => [product.id, product])),
    [products]
  );
  const visibleItems = useMemo(() => items.filter((item) => {
    const matchesMachine = !machineId || item.machineId === machineId;
    const matchesSearch = !search || item.productName.toLowerCase().includes(search.toLowerCase());
    return matchesMachine && matchesSearch;
  }), [items, machineId, search]);
  const summary = useMemo(() => {
    const units = visibleItems.reduce((sum, item) => sum + item.quantity, 0);
    const capacity = visibleItems.reduce((sum, item) => sum + item.capacity, 0);
    const value = visibleItems.reduce((sum, item) => sum + item.quantity * Number(priceByProduct[item.productId]?.price || 0), 0);
    return {
      units,
      lowStock: visibleItems.filter((item) => item.quantity <= item.lowStockThreshold).length,
      utilization: capacity ? Math.round((units / capacity) * 100) : 0,
      value
    };
  }, [visibleItems, priceByProduct]);

  return (
    <main>
      <header>
        <div>
          <h1>Inventory</h1>
          <p>Stock levels and capacity across every vending machine.</p>
        </div>
        <div className="header-actions">
          <nav aria-label="Primary navigation">
            <Link className="nav-link" href="/"><LayoutDashboard size={15} /> Dashboard</Link>
            <Link className="nav-link" href="/machines"><Cpu size={15} /> Machines</Link>
            <span className="nav-link active"><Boxes size={15} /> Inventory</span>
            <Link className="nav-link" href="/payments"><CreditCard size={15} /> Payments</Link>
            <Link className="nav-link" href="/ledger"><BookOpen size={15} /> Item ledger</Link>
          </nav>
          <button onClick={load}><RefreshCw size={16} /> Refresh</button>
        </div>
      </header>

      <section className="ledger-stats four-up">
        <div className="ledger-stat"><Warehouse /><span>Units in stock</span><strong>{summary.units}</strong></div>
        <div className="ledger-stat"><AlertTriangle /><span>Low-stock slots</span><strong>{summary.lowStock}</strong></div>
        <div className="ledger-stat"><PackageCheck /><span>Capacity used</span><strong>{summary.utilization}%</strong></div>
        <div className="ledger-stat"><CreditCard /><span>Stock value</span><strong>KES {summary.value.toLocaleString()}</strong></div>
      </section>

      <section className="panel ledger-panel">
        <div className="panel-title-row page-toolbar">
          <div><h2>Machine stock</h2><p>Low stock means the quantity is at or below its reorder threshold.</p></div>
          <div className="table-filters">
            <input aria-label="Search products" placeholder="Search products" value={search} onChange={(event) => setSearch(event.target.value)} />
            <select aria-label="Filter by machine" value={machineId} onChange={(event) => setMachineId(event.target.value)}>
              <option value="">All machines</option>
              {MACHINES.map((machine) => <option key={machine}>{machine}</option>)}
            </select>
            {!loading && <span className="record-count">{visibleItems.length} slots</span>}
          </div>
        </div>

        {error && <div className="form-error">{error}</div>}
        {loading ? <div className="empty-state">Loading inventory…</div> : visibleItems.length === 0 ? (
          <div className="empty-state"><Boxes size={28} /><strong>No inventory found</strong><span>Change the current filters and try again.</span></div>
        ) : (
          <div className="table-scroll">
            <table className="ledger-table">
              <thead><tr><th>Machine</th><th>Slot</th><th>Product</th><th>Stock</th><th>Threshold</th><th>Unit price</th><th>Stock value</th><th>State</th></tr></thead>
              <tbody>{visibleItems.map((item) => {
                const product = priceByProduct[item.productId];
                const percentage = item.capacity ? Math.round((item.quantity / item.capacity) * 100) : 0;
                const low = item.quantity <= item.lowStockThreshold;
                return (
                  <tr key={`${item.machineId}-${item.slotNumber}`}>
                    <td><strong>{item.machineId}</strong></td><td>{item.slotNumber}</td><td>{item.productName}</td>
                    <td><div className="stock-cell"><span>{item.quantity}/{item.capacity}</span><div className="stock-track"><i className={low ? 'low' : ''} style={{ width: `${Math.min(percentage, 100)}%` }} /></div></div></td>
                    <td>{item.lowStockThreshold}</td><td>{product?.currency || 'KES'} {Number(product?.price || 0).toLocaleString()}</td>
                    <td>KES {(item.quantity * Number(product?.price || 0)).toLocaleString()}</td>
                    <td><span className={`state-pill ${low ? 'low' : 'healthy'}`}>{low ? 'LOW STOCK' : 'HEALTHY'}</span></td>
                  </tr>
                );
              })}</tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}
