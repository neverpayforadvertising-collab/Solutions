const rateLimiteMap = new Map();

function rateLimiter(req, res, next) {
    const userId = req.ip;
    const now = Date.now();

    if (!rateLimitMap.has(userId)) {
        rateLimitMap.set(userId, []);
    }

    const timestamps = rateLimitMap.get(userId);

    // Keep only last 60 seconds
    const windowStart = now - 60000;
    const recentTimestamps = timestamps.filter(ts => ts > windowStart);

    if (recentTimestamps.length >= 5) {
        return res.status(429).send('Too many requests');
    }

    recentTimestamps.push(now);
    rateLimitMap.set(userId, recentTimestamps);

    next();
}

module.exports = rateLimiter;