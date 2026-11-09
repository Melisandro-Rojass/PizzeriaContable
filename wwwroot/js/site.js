/* ==========================================================================
   PIZZERIACONTABLE - SITE JAVASCRIPT
   - Preservación de posición del menú lateral (Scroll Restoration)
   - Control táctil y responsive del sidebar móvil con backdrop
   - Resaltado de navegación fluida
   ========================================================================== */

document.addEventListener("DOMContentLoaded", function () {
    const sidebar = document.getElementById("sidebarMenu");
    const backdrop = document.getElementById("sidebarBackdrop");
    const closeBtn = document.getElementById("sidebarCloseBtn");

    // 1. Restauración fluida del desplazamiento del menú lateral
    if (sidebar) {
        const STORAGE_KEY = "saycheese_sidebar_scroll_pos";
        
        // Restaurar posición previa
        const savedScroll = sessionStorage.getItem(STORAGE_KEY);
        if (savedScroll !== null) {
            sidebar.scrollTop = parseInt(savedScroll, 10);
        }

        // Guardar posición al hacer scroll en el menú
        let scrollTimeout;
        sidebar.addEventListener("scroll", function () {
            clearTimeout(scrollTimeout);
            scrollTimeout = setTimeout(function () {
                sessionStorage.setItem(STORAGE_KEY, sidebar.scrollTop);
            }, 100);
        }, { passive: true });

        // Asegurar que la opción activa sea visible si queda fuera del viewport visible
        const activeLink = sidebar.querySelector(".nav-link.active");
        if (activeLink && savedScroll === null) {
            activeLink.scrollIntoView({ block: "nearest", behavior: "smooth" });
        }
    }

    // 2. Control del Sidebar en Pantallas Móviles / Tabletas (< 992px)
    function closeMobileSidebar() {
        if (sidebar && sidebar.classList.contains("show")) {
            sidebar.classList.remove("show");
            if (backdrop) backdrop.classList.remove("show");
            document.body.classList.remove("lateral-abierto-movil");
        }
    }

    if (backdrop) {
        backdrop.addEventListener("click", closeMobileSidebar);
    }

    if (closeBtn) {
        closeBtn.addEventListener("click", closeMobileSidebar);
    }

    // Al hacer clic en un enlace de navegación en móvil, cerrar el sidebar para mostrar el contenido
    if (sidebar) {
        sidebar.querySelectorAll(".nav-link").forEach(function (link) {
            link.addEventListener("click", function () {
                if (window.innerWidth < 992) {
                    closeMobileSidebar();
                }
            });
        });
    }

    // 3. Mejorar tablas contables con auto-scroll horizontal sin romper la página
    document.querySelectorAll("table.table").forEach(function (table) {
        if (!table.parentElement.classList.contains("table-responsive")) {
            const wrapper = document.createElement("div");
            wrapper.className = "table-responsive";
            table.parentNode.insertBefore(wrapper, table);
            wrapper.appendChild(table);
        }
    });
});
