/*
  Pokemon API · documentacion
  ---------------------------------------------------------------------------
  Todo el contenido de la referencia se genera en el navegador a partir de
  /openapi.json, que es el mismo documento que consumen los generadores de
  clientes. Aqui no hay plantillas por endpoint: si mañana se añade una ruta, esta
  pagina la muestra sin tocar el fichero.

  Sin dependencias ni CDN: el script viaja incrustado en el ensamblado.
*/

(() => {
  "use strict";

  const OPENAPI_URL = "/openapi.json";

  /* ------------------------------------------------------------- utilidades */

  /** Escapa texto antes de insertarlo como HTML. */
  const escapeHtml = (value) =>
    String(value ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");

  /**
   * Convierte texto plano de OpenAPI en HTML con `<code>` en los fragmentos
   * entre acentos graves: es el formato que usan las descripciones de los
   * endpoints para nombrar rutas y parametros.
   */
  const inlineCode = (value) => escapeHtml(value).replace(/`([^`]+)`/g, "<code>$1</code>");

  /** Ancla estable por operacion, para poder enlazar a un endpoint concreto. */
  const anchorFor = (path, method) =>
    `op-${method.toLowerCase()}-${path.replaceAll(/[^a-zA-Z0-9]+/g, "-").replaceAll(/^-|-$/g, "")}`;

  /** Resuelve una referencia interna del documento. */
  const resolveRef = (document_, schema) => {
    if (!schema?.$ref) {
      return schema ?? {};
    }

    return schema.$ref.split("/").slice(1).reduce((node, part) => node?.[part], document_) ?? {};
  };

  /** Etiqueta legible para un tipo de OpenAPI. */
  const typeLabel = (schema) => {
    if (!schema) {
      return "—";
    }

    if (schema.enum?.length) {
      return "enum";
    }

    if (schema.$ref) {
      return schema.$ref.split("/").pop();
    }

    if (schema.type === "array") {
      return `array de ${typeLabel(schema.items)}`;
    }

    return schema.type ?? "—";
  };

  /**
   * Colorea un JSON highlighting keys, strings, numbers and literals.
   *
   * Se recorre el texto original token a token en lugar de escapar primero y
   * buscar entidades: un valor que contenga `<` o `&` romperia el patron.
   */
  const highlightJson = (text) => {
    const pattern = /"(?:\\.|[^"\\])*"|-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b|\b(?:true|false|null)\b/g;

    let html = "";
    let last = 0;

    for (const match of text.matchAll(pattern)) {
      const token = match[0];
      const after = text.slice(match.index + token.length);

      let kind = "number";

      if (token.startsWith('"')) {
        kind = /^\s*:/.test(after) ? "key" : "string";
      } else if (/^(?:true|false|null)$/.test(token)) {
        kind = "literal";
      }

      html += escapeHtml(text.slice(last, match.index));
      html += `<span class="${kind}">${escapeHtml(token)}</span>`;
      last = match.index + token.length;
    }

    return html + escapeHtml(text.slice(last));
  };

  /** Valor de ejemplo para previsualizar un parametro. */
  const sampleFor = (parameter) => {
    if (parameter.example !== undefined) {
      return parameter.example;
    }

    if (parameter.schema?.default !== undefined) {
      return parameter.schema.default;
    }

    if (parameter.schema?.enum?.length) {
      return parameter.schema.enum[0];
    }

    return { string: "", integer: "1", number: "1", boolean: "true" }[parameter.schema?.type] ?? "";
  };

  /* ------------------------------------------------------------------ tema */

  const initTheme = () => {
    const root = document.documentElement;
    const button = document.getElementById("theme-toggle");
    const icon = document.getElementById("theme-icon");
    const media = window.matchMedia("(prefers-color-scheme: dark)");
    const storageKey = "pokemon-api-docs-theme";

    const apply = (theme) => {
      root.dataset.theme = theme;
      icon.textContent = theme === "dark" ? "☾" : "☀";
      button.title = `Tema ${theme === "dark" ? "oscuro" : "claro"} (clic para cambiar)`;
    };

    apply(localStorage.getItem(storageKey) ?? (media.matches ? "dark" : "light"));

    button.addEventListener("click", () => {
      const next = root.dataset.theme === "dark" ? "light" : "dark";

      localStorage.setItem(storageKey, next);
      apply(next);
    });

    // Si el usuario nunca ha tocado el interruptor, se sigue al sistema.
    media.addEventListener("change", (event) => {
      if (localStorage.getItem(storageKey) === null) {
        apply(event.matches ? "dark" : "light");
      }
    });
  };

  /* ------------------------------------------------------ indice y seccion actual */

  let spy = null;

  const indexLinks = () => {
    for (const link of document.querySelectorAll("#nav a")) {
      link.dataset.search = `${link.textContent} ${link.getAttribute("href")}`.toLowerCase();
    }
  };

  const initShell = () => {
    const search = document.getElementById("filter");

    search.addEventListener("input", () => {
      const needle = search.value.trim().toLowerCase();

      for (const link of document.querySelectorAll("#nav a")) {
        link.parentElement.hidden = needle !== "" && !link.dataset.search.includes(needle);
      }
    });

    document.addEventListener("keydown", (event) => {
      if (event.key === "/" && document.activeElement !== search) {
        event.preventDefault();
        search.focus();
        search.select();
      } else if (event.key === "Escape" && document.activeElement === search) {
        search.value = "";
        search.dispatchEvent(new Event("input"));
        search.blur();
      }
    });

    // Marca en el indice la seccion que se esta leyendo.
    spy = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (!entry.isIntersecting) {
            continue;
          }

          for (const link of document.querySelectorAll("#nav a")) {
            link.classList.toggle("active", link.getAttribute("href") === `#${entry.target.id}`);
          }
        }
      },
      { rootMargin: "-70px 0px -75% 0px" },
    );

    for (const target of document.querySelectorAll("main section[id]")) {
      spy.observe(target);
    }

    indexLinks();
  };

  /* -------------------------------------------------------- ejemplos de guia */

  const initGuide = () => {
    const base = window.location.origin;
    const withOrigin = (path) => `${base}${path}`;

    const examples = {
      curl: [
        `curl -s '${withOrigin("/api/v1/pokemon?name=char&types=fire")}' \\`,
        "  -H 'Accept: application/json'",
      ].join("\n"),
      js: [
        "const respuesta = await fetch(",
        "  '/api/v1/pokemon?name=char&types=fire&pageSize=5',",
        "  { headers: { Accept: 'application/json' } },",
        ");",
        "",
        "if (!respuesta.ok) {",
        "  const error = await respuesta.json(); // RFC 9457",
        "  throw new Error(`${error.status} ${error.code}: ${error.detail}`);",
        "}",
        "",
        "const { items, page, totalCount } = await respuesta.json();",
        "console.log(`${items.length} de ${totalCount} en la página ${page}`);",
      ].join("\n"),
      python: [
        "import requests",
        "",
        "respuesta = requests.get(",
        `    '${withOrigin("/api/v1/pokemon")}',`,
        "    params={'name': 'char', 'types': 'fire', 'pageSize': 5},",
        "    headers={'Accept': 'application/json'},",
        "    timeout=10,",
        ")",
        "respuesta.raise_for_status()",
        "",
        "pagina = respuesta.json()",
        "print(f\"{len(pagina['items'])} de {pagina['totalCount']} en la página {pagina['page']}\")",
      ].join("\n"),
    };

    const target = document.getElementById("quickstart");

    const show = (language) => {
      target.innerHTML = `<code>${escapeHtml(examples[language])}</code>`;

      for (const tab of document.querySelectorAll("[data-example]")) {
        tab.setAttribute("aria-selected", String(tab.dataset.example === language));
      }
    };

    for (const tab of document.querySelectorAll("[data-example]")) {
      tab.addEventListener("click", () => show(tab.dataset.example));
    }

    show("curl");

    document.getElementById("paging-example").innerHTML = `<code>${escapeHtml(
      [
        "# Un mismo criterio de orden en todas las paginas: recorrer el catalogo",
        "# con ?page no puede duplicar ni perder elementos.",
        `curl -s '${withOrigin("/api/v1/pokemon?sortBy=baseExperience&sortDirection=desc&pageSize=5")}'`,
        "",
        "# hasNextPage=false con items=[] marca el final, no es un error.",
        `curl -s '${withOrigin("/api/v1/pokemon?page=999")}'`,
      ].join("\n"),
    )}</code>`;
  };

  /* ------------------------------------------------------- referencia OpenAPI */

  const renderParameters = (document_, parameters) => {
    if (!parameters?.length) {
      return "";
    }

    const rows = parameters
      .map((parameter) => {
        const schema = resolveRef(document_, parameter.schema);
        const enums = schema.enum ?? parameter.enum ?? [];
        const description = parameter.description ?? schema.description ?? "";
        const fallback = schema.default ?? sampleFor(parameter);

        return `<tr>
            <td><code>${escapeHtml(parameter.name)}</code></td>
            <td><code>${escapeHtml(typeLabel(schema))}</code></td>
            <td>${parameter.required ? "si" : `<span class="muted">no</span>`}${
              fallback === "" ? "" : ` <span class="muted">(${escapeHtml(fallback)})</span>`
            }</td>
            <td>
              ${description ? `<div>${inlineCode(description)}</div>` : ""}
              ${
                enums.length
                  ? `<ul class="pill-list">${enums
                      .map((value) => `<li>${escapeHtml(value)}</li>`)
                      .join("")}</ul>`
                  : ""
              }
            </td>
          </tr>`;
      })
      .join("");

    return `<div class="table-wrap" style="margin-top: 16px">
        <table>
          <thead>
            <tr><th>Parametro</th><th>Tipo</th><th>Obligatorio</th><th>Descripcion</th></tr>
          </thead>
          <tbody>${rows}</tbody>
        </table>
      </div>`;
  };

  const renderResponses = (document_, responses) => {
    const codes = Object.keys(responses ?? {}).sort();

    if (!codes.length) {
      return "";
    }

    const rows = codes
      .map((code) => {
        const response = responses[code];
        const schema = resolveRef(document_, response.content?.["application/json"]?.schema);
        const description = response.description ?? "";

        return `<tr>
            <td><code>${escapeHtml(code)}</code></td>
            <td><code>${escapeHtml(typeLabel(schema))}</code></td>
            <td>${description ? inlineCode(description) : `<span class="muted">—</span>`}</td>
          </tr>`;
      })
      .join("");

    return `<div class="table-wrap" style="margin-top: 16px">
        <table>
          <thead><tr><th>Estado</th><th>Cuerpo</th><th>Descripcion</th></tr></thead>
          <tbody>${rows}</tbody>
        </table>
      </div>`;
  };

  const renderTry = (document_, parameters) => {
    const fields = (parameters ?? [])
      .map((parameter) => {
        const schema = resolveRef(document_, parameter.schema);
        const enums = schema.enum ?? parameter.enum ?? [];
        const sample = sampleFor({ ...parameter, schema });
        const attributes = `data-param="${escapeHtml(parameter.name)}" data-in="${escapeHtml(parameter.in)}"`;

        const control = enums.length
          ? `<select ${attributes}>${enums
              .map((value) => `<option value="${escapeHtml(value)}">${escapeHtml(value)}</option>`)
              .join("")}</select>`
          : `<input type="text" ${attributes} value="${escapeHtml(sample)}" placeholder="${escapeHtml(sample)}" />`;

        return `<div class="field">
            <label>${escapeHtml(parameter.name)}${parameter.in === "path" ? " (ruta)" : ""}</label>
            ${control}
          </div>`;
      })
      .join("");

    return `<div class="try">
        <div class="try-head">
          <h4>Probar</h4>
          <span class="muted">Se ejecuta contra esta misma instancia y cuenta para el limite de uso.</span>
          <button class="button" type="button" data-try>Enviar</button>
        </div>
        <div class="try-fields">${
          fields || `<span class="muted">Este endpoint no tiene parametros.</span>`
        }</div>
        <pre class="response"><code></code></pre>
      </div>`;
  };

  const renderOperation = (document_, path, method, operation) => {
    const anchor = anchorFor(path, method);
    const summary = operation.summary ?? operation.operationId ?? "Operacion";
    const description = operation.description ?? "";
    const parameters = operation.parameters ?? [];

    return `<details class="endpoint" id="${escapeHtml(anchor)}"${
      anchor === location.hash.slice(1) ? " open" : ""
    }>
        <summary>
          <span class="method-badge get">${escapeHtml(method)}</span>
          <span class="path">${escapeHtml(path)}</span>
          <span class="summary-text">${escapeHtml(summary)}</span>
          <span class="chevron" aria-hidden="true">›</span>
        </summary>
        <div class="body">
          ${description ? `<p class="prose">${inlineCode(description)}</p>` : ""}
          ${renderParameters(document_, parameters)}
          ${renderResponses(document_, operation.responses)}
          ${renderTry(document_, parameters)}
        </div>
      </details>`;
  };

  const renderReference = (document_) => {
    const byTag = new Map();

    for (const [path, item] of Object.entries(document_.paths ?? {})) {
      for (const [method, operation] of Object.entries(item)) {
        if (method === "parameters" || typeof operation !== "object" || !operation.summary) {
          continue;
        }

        const tag = operation.tags?.[0] ?? "Otros";

        byTag.set(tag, [...(byTag.get(tag) ?? []), { path, method, operation }]);
      }
    }

    const list = document.getElementById("endpoint-list");
    const container = document.getElementById("endpoints");

    if (byTag.size === 0) {
      container.innerHTML = `<p class="empty">El documento OpenAPI no declara operaciones.</p>`;
      list.innerHTML = "";

      return;
    }

    let markup = "";
    let navMarkup = "";

    for (const [tag, operations] of byTag) {
      const tagAnchor = tag.toLowerCase();

      navMarkup += `<li><a href="#${escapeHtml(tagAnchor)}">${escapeHtml(tag)}
        <span class="muted">(${operations.length})</span></a></li>`;

      markup += `<h3 id="${escapeHtml(tagAnchor)}" style="margin: 28px 0 12px; font-size: 18px">
        ${escapeHtml(tag)}</h3>`;

      for (const { path, method, operation } of operations) {
        const anchor = anchorFor(path, method);

        navMarkup += `<li><a href="#${escapeHtml(anchor)}">
            <span class="method get">${escapeHtml(method)}</span>
            <span>${escapeHtml(path)}</span></a></li>`;

        markup += renderOperation(document_, path, method, operation);
      }
    }

    container.innerHTML = markup;
    list.innerHTML = navMarkup;

    // Los endpoints nacen despues del observador, asi que se registra aqui.
    for (const target of container.querySelectorAll(".endpoint")) {
      spy?.observe(target);
    }

    indexLinks();
  };

  /* ----------------------------------------------------------- "Probar" real */

  const initTryHandlers = () => {
    document.addEventListener("click", async (event) => {
      const button = event.target.closest("[data-try]");

      if (!button) {
        return;
      }

      const panel = button.closest(".endpoint");
      const output = panel.querySelector(".response");
      const label = button.textContent;

      button.disabled = true;
      button.textContent = "Enviando…";

      try {
        const fields = [...panel.querySelectorAll("[data-param]")].map((field) => ({
          name: field.dataset.param,
          in: field.dataset.in,
          value: field.value.trim(),
        }));

        // Los segmentos {param} se sustituyen por su valor; el resto va a la
        // query, en el orden en que se declararon los parametros.
        const route = panel
          .querySelector(".path")
          .textContent.replaceAll(/\{([^}]+)\}/g, (_, name) => {
            const field = fields.find((candidate) => candidate.name === name && candidate.in === "path");

            return encodeURIComponent(field?.value || "1");
          });

        const query = new URLSearchParams();

        for (const field of fields) {
          if (field.in === "query" && field.value !== "") {
            query.append(field.name, field.value);
          }
        }

        const url = `${window.location.origin}${route}${query.size ? `?${query}` : ""}`;
        const response = await fetch(url, { headers: { Accept: "application/json" } });
        const text = await response.text();
        let body = text || "(sin cuerpo)";

        try {
          body = JSON.stringify(JSON.parse(text), null, 2);
        } catch {
          // Una respuesta que no es JSON (por ejemplo, un 404 de proxy) se
          // muestra tal cual.
        }

        output.classList.add("visible");
        output.innerHTML = `<div class="response-head">
            <span class="${response.ok ? "status-ok" : "status-error"}">
              ${response.status} ${response.statusText}</span>
            <span class="muted">${escapeHtml(response.headers.get("content-type") ?? "")}</span>
            <span class="muted" style="margin-left:auto">${escapeHtml(url.replace(window.location.origin, ""))}</span>
          </div>
          <code class="json">${highlightJson(body)}</code>`;
      } catch (error) {
        output.classList.add("visible");
        output.innerHTML = `<div class="response-head">
            <span class="status-error">${escapeHtml(error.message)}</span>
          </div>`;
      } finally {
        button.disabled = false;
        button.textContent = label;
      }
    });
  };

  /* ------------------------------------------------------------------ inicio */

  const start = async () => {
    initTheme();
    initShell();
    initGuide();
    initTryHandlers();

    try {
      const response = await fetch(OPENAPI_URL, { headers: { Accept: "application/json" } });

      if (!response.ok) {
        throw new Error(`${response.status} ${response.statusText}`);
      }

      renderReference(await response.json());
    } catch (error) {
      document.getElementById("endpoints").innerHTML = `<p class="empty">
          No se pudo cargar <code>${escapeHtml(OPENAPI_URL)}</code>: ${escapeHtml(error.message)}.
        </p>`;
      document.getElementById("endpoint-list").innerHTML = "";
    }
  };

  document.addEventListener("DOMContentLoaded", start);
})();
