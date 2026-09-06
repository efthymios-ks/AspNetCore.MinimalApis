(function () {
    'use strict';

    function init() {
        const style = document.createElement('style');
        style.textContent = `
            .html-preview-btn {
                display: block;
                width: fit-content;
                margin: 0.5rem 0 0.5rem auto !important;
                background: #49cc90;
                color: #fff;
                border: none;
                border-radius: 0.25rem;
                padding: 0.375rem 0.875rem;
                font-size: 0.8rem;
                font-weight: bold;
                font-family: sans-serif;
                cursor: pointer;
            }
            .html-preview-btn:hover { background: #3db87a; }

            #html-preview-backdrop {
                display: none;
                position: fixed;
                inset: 0;
                background: rgba(0,0,0,0.65);
                z-index: 9999;
                align-items: center;
                justify-content: center;
            }
            #html-preview-backdrop.open { display: flex; }

            #html-preview-dialog {
                background: #fff;
                border-radius: 0.5rem;
                width: 90vw;
                height: 85vh;
                display: flex;
                flex-direction: column;
                overflow: hidden;
                box-shadow: 0 1.25rem 3.75rem rgba(0,0,0,0.45);
            }

            #html-preview-header {
                display: flex;
                align-items: center;
                gap: 0.75rem;
                padding: 0.625rem 1rem;
                background: #1b1b1b;
                color: #fff;
                font-family: sans-serif;
                font-size: 0.85rem;
                flex-shrink: 0;
            }

            #html-preview-title {
                font-weight: bold;
                white-space: nowrap;
            }

            #html-preview-url {
                flex: 1;
                font-size: 0.75rem;
                color: #aaa;
                overflow: hidden;
                text-overflow: ellipsis;
                white-space: nowrap;
            }

            #html-preview-close {
                background: none;
                border: none;
                color: #fff;
                font-size: 1.2rem;
                line-height: 1;
                padding: 0 0.25rem;
                cursor: pointer;
            }
            #html-preview-close:hover { color: #f93e3e; }

            #html-preview-iframe {
                flex: 1;
                border: none;
                width: 100%;
            }
        `;
        document.head.appendChild(style);

        const backdrop = document.createElement('div');
        backdrop.id = 'html-preview-backdrop';
        backdrop.innerHTML = `
            <div id="html-preview-dialog">
                <div id="html-preview-header">
                    <span id="html-preview-title">Preview</span>
                    <span id="html-preview-url"></span>
                    <button id="html-preview-close" title="Close">&#x2715;</button>
                </div>
                <iframe id="html-preview-iframe" sandbox="allow-same-origin allow-forms allow-scripts"></iframe>
            </div>
        `;
        document.body.appendChild(backdrop);

        const iframe = document.getElementById('html-preview-iframe');
        const closeBtn = document.getElementById('html-preview-close');
        const urlDisplay = document.getElementById('html-preview-url');

        function closeModal() {
            backdrop.classList.remove('open');
            iframe.src = 'about:blank';
        }

        closeBtn.addEventListener('click', closeModal);
        backdrop.addEventListener('click', e => { if (e.target === backdrop) closeModal(); });
        document.addEventListener('keydown', e => { if (e.key === 'Escape') closeModal(); });

        function isHtmlResponse(responseEl) {
            for (const line of responseEl.querySelectorAll('.headerline')) {
                const text = line.textContent.toLowerCase();
                if (text.includes('content-type') && text.includes('text/html')) return true;
            }
            return false;
        }

        function isGetRequest(responseEl) {
            return responseEl.closest('.opblock')?.classList.contains('opblock-get') ?? false;
        }

        function getRequestUrl(responseEl) {
            const pre = responseEl.closest('table')?.parentElement?.querySelector('.request-url pre');
            return pre?.textContent?.trim() ?? null;
        }

        function getResponseBody(responseEl) {
            const pre = responseEl.querySelector('.highlight-code pre');
            if (pre) return pre.textContent;
            return null;
        }

        function openPreview(url, body, isGet) {
            urlDisplay.textContent = url ?? '';
            if (isGet && url) {
                iframe.removeAttribute('srcdoc');
                iframe.src = url;
            } else {
                iframe.src = 'about:blank';
                iframe.srcdoc = body;
            }
            backdrop.classList.add('open');
        }

        function tryInject(responseEl) {
            if (responseEl.dataset.htmlPreviewInjected) return;
            if (!isHtmlResponse(responseEl)) return;

            const body = getResponseBody(responseEl);
            if (body === null) return;

            responseEl.dataset.htmlPreviewInjected = '1';

            const btn = document.createElement('button');
            btn.className = 'html-preview-btn';
            btn.textContent = 'Preview';
            btn.addEventListener('click', () => {
                openPreview(getRequestUrl(responseEl), getResponseBody(responseEl) ?? body, isGetRequest(responseEl));
            });

            const anchor = responseEl.querySelector('.highlight-code');
            if (anchor) {
                anchor.insertAdjacentElement('afterend', btn);
            } else {
                responseEl.querySelector('.response-col_description')?.appendChild(btn);
            }
        }

        function scan() {
            document.querySelectorAll('.live-responses-table tr.response').forEach(tryInject);
        }

        const observer = new MutationObserver(mutations => {
            if (mutations.some(m => m.addedNodes.length > 0)) scan();
        });

        observer.observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
