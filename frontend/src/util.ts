export function IsOnQuest() {
    return location.host.startsWith("127.0.0.1") || location.host.startsWith("localhost") 
}

/**
 * Sleeps for a given amount of milliseconds
 * @param ms 
 * @returns 
 */
export function Sleep(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

export function CompareStringsAlphabetically(a: string, b: string) {
    return a.localeCompare(b);
}


