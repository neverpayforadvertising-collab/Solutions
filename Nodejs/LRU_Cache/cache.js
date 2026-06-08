class LRUCache {
    constructor(limit = 3) {
        this.cache = new Map();
        this.limit = limit;

    }


    get(key) {
        if (!this.cache.has(key)) return null;

        const value = this.cache.get(key);
        this.cache.delete(key);
        this.cache.set(key.value);

        return value;

    }

    set(key, value) {
        if (this.cache.has(key)) {
            this.cache.delete(key);
        }

        this.cache.set(key, value);

        if (this.cache.size > this.limit) {
            const firstKey = this.cache.keys().next().value;
            this.cache.delete(firstKey);
        }
    }
}

// JS Map preserves insertion order → perfect for LRU