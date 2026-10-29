(() => {
  'use strict';

  const PAGE_SIZE = 12;
  const CATEGORIES = ['Printer', 'Filament', 'Scanner', 'SparePart', 'Accessory'];
  const STATUSES = ['Pending', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled'];

  // Quick-fill buttons for the seeded demo users. They only work in the Development
  // environment, where the seeder creates them. Remove this list for a real deployment.
  const DEMO_ACCOUNTS = [
    { label: 'Admin', email: 'admin@example.com', password: 'Admin123!' },
    { label: 'Sales rep', email: 'sales@example.com', password: 'Sales123!' },
    { label: 'Reseller', email: 'reseller@example.com', password: 'Reseller123!' },
  ];

  // Which actions are offered for each order status (staff only)
  const ACTIONS = {
    Pending: [
      { label: 'Confirm', path: 'confirm' },
      { label: 'Cancel order', path: 'cancel', danger: true },
    ],
    Confirmed: [
      { label: 'Ship', path: 'ship' },
      { label: 'Cancel order', path: 'cancel', danger: true },
    ],
    Shipped: [{ label: 'Mark delivered', path: 'deliver' }],
  };

  const state = {
    session: null,          // { token, expiresAt, email, role, resellerId }
    view: 'products',
    cart: [],               // [{ product, quantity }]
    resellers: [],          // staff only, used by the "Order for" dropdown
    resellerId: '',
    productFilter: { page: 1, search: '', category: '' },
    orderFilter: { page: 1, status: '' },
    openOrders: new Set(),  // ids of orders whose details are expanded
  };

  const $ = id => document.getElementById(id);
  const isStaff = () => ['Admin', 'SalesRep'].includes(state.session?.role);
  const money = n => Number(n).toLocaleString('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const when = iso => new Date(iso).toLocaleString('sv-SE', { dateStyle: 'short', timeStyle: 'short' });

  // ---------------------------------------------------------------- helpers

  // Builds DOM nodes. Text is always added as text nodes, never as HTML,
  // so data coming from the API can't inject markup.
  function h(tag, props, ...children) {
    const node = document.createElement(tag);
    for (const [key, value] of Object.entries(props || {})) {
      if (key === 'class') node.className = value;
      else if (key.startsWith('on')) node.addEventListener(key.slice(2), value);
      else if (value === true) node.setAttribute(key, '');
      else if (value !== false && value != null) node.setAttribute(key, value);
    }
    for (const child of children.flat()) {
      if (child == null || child === false) continue;
      node.append(child instanceof Node ? child : document.createTextNode(String(child)));
    }
    return node;
  }

  // Like node.replaceChildren(), but skips null/false (the DOM method would print "null")
  function fill(node, ...children) {
    node.replaceChildren(...children.flat().filter(c => c != null && c !== false));
  }

  let toastTimer;
  function toast(message, isError = false) {
    const t = $('toast');
    t.textContent = message;
    t.className = 'toast' + (isError ? ' error' : '');
    t.hidden = false;
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => { t.hidden = true; }, 4500);
  }

  function loadSession() {
    try {
      const s = JSON.parse(sessionStorage.getItem('session'));
      if (s && new Date(s.expiresAt) > new Date()) return s;
    } catch { /* storage unavailable or corrupt: start logged out */ }
    return null;
  }

  function saveSession(s) {
    try {
      if (s) sessionStorage.setItem('session', JSON.stringify(s));
      else sessionStorage.removeItem('session');
    } catch { /* storage unavailable: the session just won't survive a reload */ }
  }

  // ---------------------------------------------------------------- API

  class ApiError extends Error {
    constructor(status, message) {
      super(message);
      this.status = status;
    }
  }

  function problemText(data, status) {
    if (status === 403) return "You don't have permission to do that.";
    if (data?.detail) return data.detail;
    if (data?.errors) {
      const first = Object.values(data.errors).flat()[0];
      if (first) return first;
    }
    if (data?.title) return data.title;
    return `Request failed (${status})`;
  }

  async function api(path, { method = 'GET', body } = {}) {
    const headers = {};
    if (state.session) headers.Authorization = 'Bearer ' + state.session.token;
    if (body) headers['Content-Type'] = 'application/json';

    let res;
    try {
      res = await fetch(path, { method, headers, body: body ? JSON.stringify(body) : undefined });
    } catch {
      throw new ApiError(0, 'Cannot reach the API.');
    }

    if (res.status === 401 && state.session) {
      logout();
      throw new ApiError(401, 'Your session has expired. Please log in again.');
    }
    if (res.status === 204) return null;

    const data = await res.json().catch(() => null);
    if (!res.ok) throw new ApiError(res.status, problemText(data, res.status));
    return data;
  }

  // ---------------------------------------------------------------- session

  function logout() {
    state.session = null;
    state.cart = [];
    state.openOrders.clear();
    saveSession(null);
    render();
  }

  function renderLogin() {
    const error = h('p', { class: 'error', role: 'alert' });
    const email = h('input', { type: 'email', id: 'email', required: true, autocomplete: 'username' });
    const password = h('input', { type: 'password', id: 'password', required: true, autocomplete: 'current-password' });
    const submit = h('button', { type: 'submit' }, 'Log in');

    const form = h('form', {
      class: 'card login',
      onsubmit: async e => {
        e.preventDefault();
        error.textContent = '';
        submit.disabled = true;
        try {
          const r = await api('/auth/login', { method: 'POST', body: { email: email.value, password: password.value } });
          state.session = { token: r.token, expiresAt: r.expiresAt, email: r.email, role: r.role, resellerId: r.resellerId };
          saveSession(state.session);
          state.view = 'products';
          render();
        } catch (err) {
          error.textContent = err.message;
          submit.disabled = false;
        }
      },
    },
      h('h1', {}, 'Reseller Order Portal'),
      h('p', { class: 'muted' }, 'Log in to browse products and manage orders.'),
      h('label', { for: 'email' }, 'Email'), email,
      h('label', { for: 'password' }, 'Password'), password,
      error,
      submit,
      h('div', { class: 'demo' },
        h('span', { class: 'muted' }, 'Demo accounts (development only):'),
        DEMO_ACCOUNTS.map(a => h('button', {
          type: 'button',
          class: 'ghost',
          onclick: () => { email.value = a.email; password.value = a.password; },
        }, a.label))));

    $('app').replaceChildren(form);
  }

  // ---------------------------------------------------------------- shared pieces

  function pager(page, pageSize, total, onChange) {
    const pages = Math.max(1, Math.ceil(total / pageSize));
    return h('div', { class: 'pager' },
      h('button', { class: 'ghost', disabled: page <= 1, onclick: () => onChange(page - 1) }, 'Previous'),
      h('span', {}, `Page ${page} of ${pages} · ${total} total`),
      h('button', { class: 'ghost', disabled: page >= pages, onclick: () => onChange(page + 1) }, 'Next'));
  }

  const statusBadge = status => h('span', { class: 'badge status-' + status.toLowerCase() }, status);
  const headRow = (...cols) => h('thead', {}, h('tr', {}, cols.map(([text, num, cls]) => h('th', { class: [num ? 'num' : '', cls || ''].join(' ').trim() }, text))));

  // ---------------------------------------------------------------- products + cart

  function renderProducts() {
    const f = state.productFilter;
    const table = h('div', { class: 'table-wrap' });

    const search = h('input', { type: 'search', placeholder: 'Search name or EAN', value: f.search, 'aria-label': 'Search products' });
    const category = h('select', { 'aria-label': 'Category' },
      h('option', { value: '' }, 'All categories'),
      CATEGORIES.map(c => h('option', { value: c, selected: c === f.category }, c)));

    let timer;
    search.addEventListener('input', () => {
      clearTimeout(timer);
      timer = setTimeout(() => { f.search = search.value.trim(); f.page = 1; loadProducts(table); }, 300);
    });
    category.addEventListener('change', () => { f.category = category.value; f.page = 1; loadProducts(table); });

    const cartPanel = h('aside', { class: 'card cart' });
    $('app').replaceChildren(h('div', { class: 'layout' },
      h('section', {}, h('h2', {}, 'Products'), h('div', { class: 'filters' }, search, category), table),
      cartPanel));

    renderCart(cartPanel);
    loadProducts(table);
    if (isStaff() && state.resellers.length === 0) loadResellers().then(() => renderCart(cartPanel));
  }

  async function loadProducts(container) {
    const f = state.productFilter;
    const q = new URLSearchParams({ page: f.page, pageSize: PAGE_SIZE });
    if (f.search) q.set('search', f.search);
    if (f.category) q.set('category', f.category);

    container.replaceChildren(h('p', { class: 'muted' }, 'Loading…'));
    try {
      const data = await api('/products?' + q);
      if (data.items.length === 0) {
        container.replaceChildren(h('p', { class: 'muted' }, 'No products match.'));
        return;
      }
      const rows = data.items.map(p => h('tr', {},
        h('td', {}, h('div', { class: 'name' }, p.name), h('div', { class: 'muted small' }, 'EAN ' + p.ean)),
        h('td', { class: 'hide-sm' }, p.category),
        h('td', { class: 'num' }, money(p.netPrice)),
        h('td', { class: 'num' }, p.stockQuantity === 0 ? h('span', { class: 'badge status-cancelled' }, 'Out of stock') : p.stockQuantity),
        h('td', { class: 'num' }, h('button', { onclick: () => addToCart(p) }, 'Add'))));

      container.replaceChildren(
        h('table', {}, headRow(['Product'], ['Category', false, 'hide-sm'], ['Price', true], ['In stock', true], ['']), h('tbody', {}, rows)),
        pager(data.page, data.pageSize, data.totalCount, p => { f.page = p; loadProducts(container); }));
    } catch (err) {
      container.replaceChildren(h('p', { class: 'error' }, err.message));
    }
  }

  async function loadResellers() {
    try {
      // The API caps a page at 100 resellers, which is plenty for this demo
      const data = await api('/resellers?pageSize=100');
      state.resellers = data.items;
      if (!state.resellerId && data.items.length) state.resellerId = data.items[0].resellerId;
    } catch (err) {
      toast(err.message, true);
    }
  }

  function addToCart(product) {
    const line = state.cart.find(l => l.product.productId === product.productId);
    if (line) line.quantity = Math.min(line.quantity + 1, 10000);
    else state.cart.push({ product, quantity: 1 });
    renderCart(document.querySelector('.cart'));
  }

  function renderCart(panel) {
    if (!panel) return;
    const total = state.cart.reduce((sum, l) => sum + l.quantity * l.product.netPrice, 0);

    const lines = state.cart.map(l => h('li', {},
      h('span', { class: 'grow' }, l.product.name),
      h('input', {
        type: 'number', min: 1, max: 10000, value: l.quantity,
        'aria-label': 'Quantity for ' + l.product.name,
        onchange: e => {
          const n = Math.floor(Number(e.target.value));
          l.quantity = Math.min(Math.max(n || 1, 1), 10000);
          renderCart(panel);
        },
      }),
      h('button', {
        class: 'ghost',
        'aria-label': 'Remove ' + l.product.name,
        onclick: () => { state.cart = state.cart.filter(x => x !== l); renderCart(panel); },
      }, '✕')));

    const reseller = isStaff()
      ? h('label', {}, 'Order for',
          h('select', { onchange: e => { state.resellerId = e.target.value; } },
            state.resellers.map(r => h('option', { value: r.resellerId, selected: r.resellerId === state.resellerId }, `${r.name} (${r.country})`))))
      : null;

    const place = h('button', { disabled: state.cart.length === 0, onclick: () => placeOrder(place) }, 'Place order');

    fill(panel,
      h('h2', {}, 'New order'),
      state.cart.length
        ? h('ul', { class: 'cart-lines' }, lines)
        : h('p', { class: 'muted' }, 'Add products to start an order.'),
      reseller,
      h('div', { class: 'total' }, h('span', {}, 'Estimated total'), h('strong', {}, money(total))),
      place);
  }

  async function placeOrder(button) {
    const resellerId = isStaff() ? state.resellerId : state.session.resellerId;
    if (!resellerId) { toast('Choose a reseller first.', true); return; }

    button.disabled = true;
    try {
      const order = await api('/orders', {
        method: 'POST',
        body: { resellerId, lines: state.cart.map(l => ({ productId: l.product.productId, quantity: l.quantity })) },
      });
      state.cart = [];
      state.view = 'orders';
      state.orderFilter = { page: 1, status: '' };
      state.openOrders = new Set([order.orderId]);
      toast(`Order placed. Total ${money(order.totalAmount)}`);
      render();
    } catch (err) {
      toast(err.message, true);
      button.disabled = false;
    }
  }

  // ---------------------------------------------------------------- orders

  function renderOrders() {
    const f = state.orderFilter;
    const table = h('div', { class: 'table-wrap' });
    const status = h('select', {
      'aria-label': 'Status',
      onchange: e => { f.status = e.target.value; f.page = 1; loadOrders(table); },
    },
      h('option', { value: '' }, 'All statuses'),
      STATUSES.map(s => h('option', { value: s, selected: s === f.status }, s)));

    $('app').replaceChildren(h('section', {},
      h('h2', {}, isStaff() ? 'Orders' : 'My orders'),
      h('div', { class: 'filters' }, status),
      table));
    loadOrders(table);
  }

  async function loadOrders(container) {
    const f = state.orderFilter;
    const q = new URLSearchParams({ page: f.page, pageSize: 10 });
    if (f.status) q.set('status', f.status);

    container.replaceChildren(h('p', { class: 'muted' }, 'Loading…'));
    try {
      const data = await api('/orders?' + q);
      if (data.items.length === 0) {
        container.replaceChildren(h('p', { class: 'muted' }, 'No orders found.'));
        return;
      }

      const rows = [];
      for (const o of data.items) {
        const open = state.openOrders.has(o.orderId);
        const detailCell = h('td', { colspan: 6 });
        rows.push(h('tr', {},
          h('td', {}, when(o.createdAt)),
          h('td', {}, o.resellerName),
          h('td', {}, statusBadge(o.status)),
          h('td', { class: 'num' }, o.lineCount),
          h('td', { class: 'num' }, money(o.totalAmount)),
          h('td', { class: 'num' }, h('button', {
            class: 'ghost',
            'aria-expanded': String(open),
            onclick: () => {
              if (open) state.openOrders.delete(o.orderId); else state.openOrders.add(o.orderId);
              loadOrders(container);
            },
          }, open ? 'Hide' : 'Details'))));
        if (open) {
          rows.push(h('tr', { class: 'detail-row' }, detailCell));
          loadOrderDetail(o.orderId, detailCell, container);
        }
      }

      container.replaceChildren(
        h('table', {}, headRow(['Created'], ['Reseller'], ['Status'], ['Lines', true], ['Total', true], ['']), h('tbody', {}, rows)),
        pager(data.page, data.pageSize, data.totalCount, p => { f.page = p; loadOrders(container); }));
    } catch (err) {
      container.replaceChildren(h('p', { class: 'error' }, err.message));
    }
  }

  async function loadOrderDetail(id, cell, container) {
    cell.replaceChildren(h('span', { class: 'muted' }, 'Loading…'));
    try {
      const order = await api('/orders/' + id);
      const actions = isStaff()
        ? (ACTIONS[order.status] || []).map(a => h('button', {
            class: a.danger ? 'danger' : '',
            onclick: () => runAction(order, a, container),
          }, a.label))
        : [];

      fill(cell,
        h('table', { class: 'inner' },
          headRow(['Product'], ['Quantity', true], ['Unit price', true], ['Line total', true]),
          h('tbody', {}, order.lines.map(l => h('tr', {},
            h('td', {}, l.productName),
            h('td', { class: 'num' }, l.quantity),
            h('td', { class: 'num' }, money(l.unitPrice)),
            h('td', { class: 'num' }, money(l.lineTotal)))))),
        actions.length ? h('div', { class: 'actions' }, actions) : null);
    } catch (err) {
      cell.replaceChildren(h('span', { class: 'error' }, err.message));
    }
  }

  async function runAction(order, action, container) {
    if (action.danger && !window.confirm('Cancel this order? This cannot be undone.')) return;
    try {
      await api(`/orders/${order.orderId}/${action.path}`, { method: 'POST' });
      toast(`${action.label}: done`);
    } catch (err) {
      toast(err.message, true);   // for example "Insufficient stock for: ..."
    }
    loadOrders(container);
  }

  // ---------------------------------------------------------------- start

  function render() {
    const s = state.session;
    $('topbar').hidden = !s;
    if (!s) { renderLogin(); return; }

    $('who').textContent = `${s.email} · ${s.role}`;
    document.querySelectorAll('.tab').forEach(t =>
      t.setAttribute('aria-current', String(t.dataset.view === state.view)));

    if (state.view === 'orders') renderOrders();
    else renderProducts();
  }

  document.querySelectorAll('.tab').forEach(t =>
    t.addEventListener('click', () => { state.view = t.dataset.view; render(); }));
  $('logout').addEventListener('click', logout);

  state.session = loadSession();
  render();
})();
