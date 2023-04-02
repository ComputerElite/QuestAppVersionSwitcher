export function IsOnQuest() {
    return location.host.startsWith("127.0.0.1") || location.host.startsWith("localhost") 
}
