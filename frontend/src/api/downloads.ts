type getDownloadsResponse = Array<{
    percentage: number;
    percentageString: string;
    done: number;
    total: number;
    speed: number;
    eTASeconds: number;
    doneString: string;
    totalString: string;
    speedString: string;
    eTAString: string;
    name: string;
    backupName: string;
    textColor: string;
}>

export async function getDownloads(): Promise<getDownloadsResponse> {
    let result = await fetch("/api/downloads");
    return await result.json();
}
