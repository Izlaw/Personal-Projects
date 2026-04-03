/**
 * Supabase JS interop for PersonalProjects Blazor WASM.
 */

const SUPABASE_URL = 'https://jvljcrwazmlcjkqomwdz.supabase.co';
const SUPABASE_KEY = 'sb_publishable_pI-fJ6da4F2-D0p3qOBTmw_cp_BNKhX';

function _client() {
    if (!window._supabaseClient) {
        window._supabaseClient = window.supabase.createClient(SUPABASE_URL, SUPABASE_KEY);
    }
    return window._supabaseClient;
}

function _mapDtr(row) {
    return {
        Key: row.id,
        Date: row.date ?? '',
        MorningLogin: row.morning_login ?? '',
        MorningLogout: row.morning_logout ?? '',
        AfternoonLogin: row.afternoon_login ?? '',
        AfternoonLogout: row.afternoon_logout ?? '',
        Hours: row.hours ?? 0
    };
}

function _mapTab(row) {
    return {
        Key: row.id,
        Title: row.title ?? 'Untitled',
        Content: row.content ?? '',
        Order: row.tab_order ?? 0,
        CreatedAt: row.created_at ?? 0
    };
}

window.dbFunctions = {

    // ─── DTR ──────────────────────────────────────────────────────────────────
    dtr: {
        _channel: null,

        init: async function (dotNetRef) {
            const db = _client();

            // Initial load
            const { data } = await db.from('dtr_entries').select('*').order('date');
            if (data) dotNetRef.invokeMethodAsync('UpdateEntries', JSON.stringify(data.map(_mapDtr)));

            // Real-time updates
            this._channel = db.channel('dtr_entries')
                .on('postgres_changes', { event: '*', schema: 'public', table: 'dtr_entries' }, async () => {
                    const { data } = await db.from('dtr_entries').select('*').order('date');
                    if (data) dotNetRef.invokeMethodAsync('UpdateEntries', JSON.stringify(data.map(_mapDtr)));
                })
                .subscribe();
        },

        saveEntry: async function (key, entryJson) {
            const e = JSON.parse(entryJson);
            await _client().from('dtr_entries').update({
                date: e.Date,
                morning_login: e.MorningLogin,
                morning_logout: e.MorningLogout,
                afternoon_login: e.AfternoonLogin,
                afternoon_logout: e.AfternoonLogout,
                hours: e.Hours
            }).eq('id', key);
        },

        pushEntry: async function (entryJson) {
            const e = JSON.parse(entryJson);
            const { data } = await _client().from('dtr_entries').insert({
                date: e.Date,
                morning_login: e.MorningLogin,
                morning_logout: e.MorningLogout,
                afternoon_login: e.AfternoonLogin,
                afternoon_logout: e.AfternoonLogout,
                hours: e.Hours
            }).select().single();
            return data?.id ?? null;
        },

        deleteEntry: async function (key) {
            await _client().from('dtr_entries').delete().eq('id', key);
        },

        detach: function () {
            if (this._channel) _client().removeChannel(this._channel);
        }
    },

    // ─── Notepad ───────────────────────────────────────────────────────────────
    notepad: {
        _channel: null,
        _sortable: null,
        _keyHandler: null,

        init: async function (dotNetRef) {
            const db = _client();

            // Initial load
            const { data } = await db.from('notepad_tabs').select('*').order('tab_order');
            if (data) dotNetRef.invokeMethodAsync('UpdateTabs', JSON.stringify(data.map(_mapTab)));

            // Real-time updates
            this._channel = db.channel('notepad_tabs')
                .on('postgres_changes', { event: '*', schema: 'public', table: 'notepad_tabs' }, async () => {
                    const { data } = await db.from('notepad_tabs').select('*').order('tab_order');
                    if (data) dotNetRef.invokeMethodAsync('UpdateTabs', JSON.stringify(data.map(_mapTab)));
                })
                .subscribe();
        },

        saveTab: async function (key, tabJson) {
            const t = JSON.parse(tabJson);
            await _client().from('notepad_tabs').update({
                title: t.Title,
                content: t.Content,
                tab_order: t.Order,
                last_modified: Date.now()
            }).eq('id', key);
        },

        pushTab: async function (tabJson) {
            const t = JSON.parse(tabJson);
            const { data } = await _client().from('notepad_tabs').insert({
                title: t.Title,
                content: t.Content ?? '',
                tab_order: t.Order,
                created_at: Date.now(),
                last_modified: Date.now()
            }).select().single();
            return data?.id ?? null;
        },

        deleteTab: async function (key) {
            await _client().from('notepad_tabs').delete().eq('id', key);
        },

        initSortable: function (dotNetRef) {
            const container = document.getElementById('notepad-tab-bar');
            if (!container || typeof Sortable === 'undefined') return;
            if (this._sortable) this._sortable.destroy();
            this._sortable = new Sortable(container, {
                animation: 150,
                draggable: '.notepad-tab-chip',
                onEnd: (evt) => {
                    dotNetRef.invokeMethodAsync('OnTabReordered', evt.item.dataset.tabKey, evt.newIndex);
                }
            });
        },

        addKeyListeners: function (dotNetRef) {
            this._keyHandler = (e) => {
                if (e.ctrlKey && e.key === 's') { e.preventDefault(); dotNetRef.invokeMethodAsync('SaveCurrentTab'); }
                else if (e.ctrlKey && e.key === 'n') { e.preventDefault(); dotNetRef.invokeMethodAsync('CreateNewTab'); }
                else if (e.ctrlKey && e.key === 'w') { e.preventDefault(); dotNetRef.invokeMethodAsync('CloseCurrentTab'); }
            };
            document.addEventListener('keydown', this._keyHandler);
        },

        removeKeyListeners: function () {
            if (this._keyHandler) {
                document.removeEventListener('keydown', this._keyHandler);
                this._keyHandler = null;
            }
        },

        detach: function () {
            if (this._channel) _client().removeChannel(this._channel);
            this.removeKeyListeners();
            if (this._sortable) { this._sortable.destroy(); this._sortable = null; }
        }
    }
};

window.reloadPage = async () => window.location.reload();
