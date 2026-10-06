
let fieldTypes = [];
let currentTable = null;

const $ = (id) => document.getElementById(id);

async function api(method, url, body) {
  const options = { method, headers: {} };
  if (body !== undefined) {
    options.headers["Content-Type"] = "application/json";
    options.body = JSON.stringify(body);
  }
  let response;
  try {
    response = await fetch(url, options);
  } catch (e) {
    return { ok: false, error: "Немає зв'язку з сервером" };
  }
  let data = null;
  try { data = await response.json(); } catch (e) {  }
  if (!response.ok) {
    return { ok: false, error: (data && data.error) || ("Помилка " + response.status) };
  }
  return { ok: true, data };
}

function setStatus(text, isError) {
  const el = $("status");
  el.textContent = text;
  el.className = isError ? "error-status" : "";
}

function el(tag, props, children) {
  const node = document.createElement(tag);
  if (props) Object.assign(node, props);
  if (children) for (const c of children) node.appendChild(c);
  return node;
}

function openDialog(title, bodyNodes, onSubmit, okText) {
  $("dialogTitle").textContent = title;
  $("dialogError").textContent = "";
  $("dialogOk").textContent = okText || "OK";
  const body = $("dialogBody");
  body.replaceChildren(...bodyNodes);

  const dialog = $("dialog");
  $("dialogForm").onsubmit = async (e) => {
    e.preventDefault();
    const error = await onSubmit();
    if (error) $("dialogError").textContent = error;
    else dialog.close();
  };
  $("dialogCancel").onclick = () => dialog.close();
  dialog.showModal();
}

function showNoDatabase() {
  $("dbName").textContent = "База не відкрита";
  for (const id of ["btnNewDb", "btnLoad", "btnSave"]) $(id).hidden = true;
  document.querySelector("aside").hidden = true;
  currentTable = null;

  const create = el("button", { className: "primary", textContent: "Створити базу" });
  create.onclick = () => $("btnNewDb").onclick();
  const open = el("button", { className: "secondary", textContent: "Відкрити" });
  open.onclick = () => $("btnLoad").onclick();
  $("content").replaceChildren(el("div", { className: "card" }, [
    el("p", { className: "muted", textContent: "База не відкрита. Створіть нову базу або відкрийте збережену." }),
    create, document.createTextNode(" "), open
  ]));
}

function showDatabaseControls() {
  for (const id of ["btnNewDb", "btnLoad", "btnSave"]) $(id).hidden = false;
  document.querySelector("aside").hidden = false;
}

async function refreshDb() {
  const r = await api("GET", "/api/db");
  if (!r.ok) return setStatus(r.error, true);

  if (r.data.exists === false) return showNoDatabase();
  showDatabaseControls();

  $("dbName").textContent = "База: " + r.data.name;
  const list = $("tableList");
  list.replaceChildren();

  let stillExists = false;
  for (const t of r.data.tables) {
    if (t.name === currentTable) stillExists = true;
    const li = el("li", { className: t.name === currentTable ? "active" : "" });
    const label = el("span", { textContent: t.name }, [
      el("span", { className: "count", textContent: "(" + t.rowCount + ")" })
    ]);
    const drop = el("button", { className: "drop", textContent: "✕", title: "Знищити таблицю" });
    drop.onclick = (e) => { e.stopPropagation(); dropTable(t.name); };
    li.onclick = () => { currentTable = t.name; refreshDb(); };
    li.append(label, drop);
    list.appendChild(li);
  }
  if (!stillExists) currentTable = null;
  await showTable();
}

function displayValue(type, value) {
  if (type === "textFile") {
    const nl = value.indexOf("\n");
    return (nl >= 0 ? value.substring(0, nl) : value) + " (текстовий файл)";
  }
  return value;
}

async function showTable() {
  const content = $("content");
  if (!currentTable) {
    content.replaceChildren(el("div", { className: "card" }, [
      el("p", { className: "muted", textContent: "Оберіть таблицю зліва або створіть нову." })
    ]));
    return;
  }
  const r = await api("GET", "/api/tables/" + encodeURIComponent(currentTable));
  if (!r.ok) return setStatus(r.error, true);
  const table = r.data;

  const addBtn = el("button", { className: "primary", textContent: "+ Додати рядок" });
  addBtn.onclick = () => openRowForm(table, null, null);

  const toolbar = el("div", { className: "toolbar" }, [
    el("h2", { textContent: table.name }), addBtn,
    el("span", { className: "badge soft", textContent: "Рядків: " + table.rows.length })
  ]);

  const headRow = el("tr");
  headRow.appendChild(el("th", { textContent: "#" }));
  for (const c of table.columns) {
    headRow.appendChild(el("th", {}, [
      document.createTextNode(c.name + " "),
      el("small", { textContent: "(" + c.type + ")" })
    ]));
  }
  headRow.appendChild(el("th"));

  const tbody = el("tbody");
  table.rows.forEach((row, index) => {
    const tr = el("tr");
    tr.appendChild(el("td", { textContent: String(index + 1) }));
    row.forEach((value, i) => {
      tr.appendChild(el("td", { textContent: displayValue(table.columns[i].type, value) }));
    });
    const edit = el("button", { textContent: "Редагувати" });
    edit.onclick = () => openRowForm(table, index, row);
    const del = el("button", { className: "destructive", textContent: "Видалити" });
    del.onclick = () => deleteRow(index);
    tr.appendChild(el("td", { className: "actions" }, [edit, document.createTextNode(" "), del]));
    tbody.appendChild(tr);
  });

  const grid = el("table", {}, [el("thead", {}, [headRow]), tbody]);
  content.replaceChildren(toolbar, el("div", { className: "card table-wrap" }, [grid]));
}

$("btnNewDb").onclick = () => {
  const input = el("input", { type: "text", value: "Нова база" });
  openDialog("Нова база", [fieldBlock("Назва бази", input)], async () => {
    const r = await api("POST", "/api/db", { name: input.value });
    if (!r.ok) return r.error;
    currentTable = null;
    setStatus("Створено нову базу");
    refreshDb();
    return null;
  });
};

function askFileName(title, okText, action) {
  const input = el("input", { type: "text", value: "database.json" });
  const hint = el("div", { className: "hint", textContent: "Файл зберігається в папці data на сервері" });
  openDialog(title, [fieldBlock("Ім'я файлу", input, hint)], async () => {
    const r = await api("POST", action, { fileName: input.value });
    if (!r.ok) return r.error;
    setStatus(okText + ": " + input.value);
    currentTable = null;
    refreshDb();
    return null;
  }, "OK");
}

$("btnSave").onclick = () => askFileName("Зберегти базу", "Збережено", "/api/db/save");
$("btnLoad").onclick = () => askFileName("Відкрити базу", "Відкрито", "/api/db/load");

function fieldBlock(labelText, input, hintNode) {
  const block = el("div", { className: "field" }, [el("label", { textContent: labelText }), input]);
  if (hintNode) block.appendChild(hintNode);
  return block;
}

$("btnCreateTable").onclick = () => {
  const nameInput = el("input", { type: "text" });
  const columnsBox = el("div");

  function addColumnRow() {
    const nameBox = el("input", { type: "text", placeholder: "Назва колонки" });
    const select = el("select");
    for (const t of fieldTypes) select.appendChild(el("option", { value: t.name, textContent: t.name }));
    select.value = "string";
    const remove = el("button", { type: "button", textContent: "✕" });
    const row = el("div", { className: "col-row" }, [nameBox, select, remove]);
    remove.onclick = () => row.remove();
    columnsBox.appendChild(row);
  }
  addColumnRow();

  const addBtn = el("button", { type: "button", textContent: "+ Колонка" });
  addBtn.onclick = addColumnRow;

  openDialog("Нова таблиця", [
    fieldBlock("Назва таблиці", nameInput),
    el("label", { className: "dialog-label", textContent: "Колонки" }),
    columnsBox, addBtn
  ], async () => {
    const columns = [];
    for (const row of columnsBox.children) {
      columns.push({ name: row.children[0].value, type: row.children[1].value });
    }
    const r = await api("POST", "/api/tables", { name: nameInput.value, columns });
    if (!r.ok) return r.error;
    currentTable = nameInput.value.trim();
    setStatus("Таблицю створено");
    refreshDb();
    return null;
  }, "Створити");
};

async function dropTable(name) {
  if (!confirm("Знищити таблицю '" + name + "' разом з усіма рядками?")) return;
  const r = await api("DELETE", "/api/tables/" + encodeURIComponent(name));
  if (!r.ok) return setStatus(r.error, true);
  if (currentTable === name) currentTable = null;
  setStatus("Таблицю знищено");
  refreshDb();
}

function openRowForm(table, rowIndex, existingRow) {
  const getters = [];
  const nodes = [];

  table.columns.forEach((col, i) => {
    const initial = existingRow ? existingRow[i] : "";
    let input;
    let extra = null;

    if (col.type === "textFile") {
      let fileValue = initial;
      const nameLabel = el("span", {
        className: "file-name",
        textContent: initial ? initial.split("\n")[0] : "(файл не обрано)"
      });
      input = el("input", { type: "file", accept: ".txt,text/plain" });
      input.onchange = () => {
        const file = input.files[0];
        if (!file) return;
        const reader = new FileReader();
        reader.onload = () => {
          fileValue = file.name + "\n" + String(reader.result).replace(/\r\n/g, "\n");
          nameLabel.textContent = file.name;
        };
        reader.readAsText(file);
      };
      extra = nameLabel;
      getters.push(() => fileValue);
    } else {
      input = el("input", { type: "text", value: initial });
      getters.push(() => input.value);
    }

    const block = el("div", { className: "field" }, [
      el("label", { textContent: col.name + " (" + col.type + ")" }), input
    ]);
    if (extra) block.appendChild(extra);
    block.appendChild(el("div", { className: "hint", textContent: col.hint }));
    nodes.push(block);
  });

  const title = existingRow ? "Редагування рядка" : "Новий рядок";
  openDialog(title, nodes, async () => {
    const values = getters.map((g) => g());
    const url = "/api/tables/" + encodeURIComponent(table.name) + "/rows" +
                (existingRow ? "/" + rowIndex : "");
    const r = await api(existingRow ? "PUT" : "POST", url, { values });
    if (!r.ok) return r.error;
    setStatus(existingRow ? "Рядок оновлено" : "Рядок додано");
    refreshDb();
    return null;
  });
}

async function deleteRow(index) {
  const r = await api("DELETE", "/api/tables/" + encodeURIComponent(currentTable) + "/rows/" + index);
  if (!r.ok) return setStatus(r.error, true);
  setStatus("Рядок видалено");
  refreshDb();
}

$("btnProduct").onclick = async () => {
  const r = await api("GET", "/api/db");
  if (!r.ok) return setStatus(r.error, true);
  if (r.data.exists === false) return showNoDatabase();
  const names = r.data.tables.map((t) => t.name);
  if (names.length < 2) return setStatus("Для добутку потрібно щонайменше дві таблиці", true);

  function makeSelect(selectedIndex) {
    const s = el("select");
    for (const n of names) s.appendChild(el("option", { value: n, textContent: n }));
    s.selectedIndex = selectedIndex;
    return s;
  }
  const selA = makeSelect(0);
  const selB = makeSelect(1);
  const resultInput = el("input", { type: "text" });

  openDialog("Прямий добуток", [
    fieldBlock("Таблиця A", selA),
    fieldBlock("Таблиця B", selB),
    fieldBlock("Назва таблиці-результату", resultInput)
  ], async () => {
    const res = await api("POST", "/api/operations/product",
      { tableA: selA.value, tableB: selB.value, resultName: resultInput.value });
    if (!res.ok) return res.error;
    currentTable = resultInput.value.trim();
    setStatus("Прямий добуток виконано");
    refreshDb();
    return null;
  }, "Виконати");
};

(async function init() {
  const r = await api("GET", "/api/types");
  if (r.ok) fieldTypes = r.data;
  await refreshDb();
})();
