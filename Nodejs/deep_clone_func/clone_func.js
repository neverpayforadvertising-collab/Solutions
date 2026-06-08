function deepClone(obj, seen = new WeakMap()) {

    if(obj === null || typeof obj !== 'object') {
        return obj;
    }   

    if(seen.has(obj)) {
        return seen.get(obj);
    }

    const clone = Array.isArray(obj) ? [] : {};
    seen.set(obj, clone);

    for (const key in obj) {
        clone[key] = deepClone(obj[key], seen);
    }
    return clone;
}

export default deepClone;