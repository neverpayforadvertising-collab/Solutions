async function runTasks(tasks, limit = 2) {

    let index = 0;

    async function worker() {
        while (index < tasks.length) {
            const currentIndex = index++;
            await tasks[currentIndex]();
        }
    }

    const workers = Array.from({ length: limit }, worker);
    await Promise.all(workers);
}


const tasks = [
    () => fetch("https://api1.com"),
    () => fetch("https://api2.com"),
    () => fetch("https://api3.com"),
];

runTasks(tasks, 2);


