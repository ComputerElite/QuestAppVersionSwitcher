import toast from "solid-toast";

export interface IDownloadManagerInfo {
  backupName: string;
  /**
   * Amount of bytes downloaded
   */
  done: number;
  /**
   * Preformatted string of the amount of bytes downloaded
   * Example: "871.2 MB"
   */
  doneString: string;
  /**
   * Estimated time of completion in seconds
   */
  eTASeconds: number;
  /**
   * Preformatted string of the estimated time of completion
   */
  eTAString: string;
  /**
   * Is the download cancelable
   */
  isCancelable: boolean;
  /**
   * Name of the game with version ex: "Beat Saber 1.29.4_4821066007"
   */
  name: string;
  /**
   * Package name of the game ex: "com.beatgames.beatsaber"
   */
  packageName: string;
  /**
   * Progress of the download as a number from 0 to 1
   */
  percentage: number;
  /**
   * Preformatted string of the progress of the download
   */
  percentageString: string;
  /**
   * Speed of the download in bytes per second
   */
  speed: number;
  /**
   * Preformatted string of the speed of the download
   * Example: "5 MB/s"
   */
  speedString: string;
  /**
   * Name of the file being downloaded?
   * Example: "Beat Saber.apk"
   */
  text: string;
  /**
   * Color of the text in hex
   * Example: "#EEEEEE"
   */
  textColor: string;
  /**
   * Total amount of bytes to download
   */
  total: number;
  /**
   * Preformatted string of the total amount of bytes to download
   * Example: "908.96 MB"
   */
  totalString: string;
  /**
   * Version of the game ex: "1.29.4_4821066007"
   */
  version: string;
}

export interface IGameDownloadItem {
  /**
   * Name of the backup
   */
  backupName: string;
  /**
   * Canceled
   */
  canceled: boolean;
  /**
   * Backup is done
   */
  done: boolean;
  /**
   * download managers array it contains the info about the download, usually only one item
   */
  downloadManagers: Array<IDownloadManagerInfo>;
  /**
   * Error if the account does not own the game
   */
  entitlementError: boolean;
  /**
   * Is the download errored
   */
  error: boolean;
  /**
   * Amount of files downloaded
   */
  filesDownloaded: number;
  /**
   * Amount of files to download
   */
  filesToDownload: number;
  /**
   * Name of the game ex: "Beat Saber"
   */
  gameName: string;
  /**
   * Id of the download
   */
  id: string;
  maxConcurrentConnections: number;
  maxConcurrentDownloads: number;
  obbsToDo: any[];
  packageName: string;
  /**
   * Progress of the download as a number from 0 to 1
   */
  progress: number;
  /**
   * Preformatted progress string
   */
  progressString: string;

  status: string;
  textColor: string;
  /**
   * Version of the game ex: "1.13.2"
   */
  version: string;
}

export interface IAPIDownloadsResponse {
  gameDownloads: IGameDownloadItem[];
  individualDownloads: IGameDownloadItem[];
}

export async function getDownloads(): Promise<IAPIDownloadsResponse> {
  let result = await fetch("/api/downloads");
  return await result.json();
}

interface downloadRequest {
  binaryId: string;
  password: string;
  version: string;
  app: string;
  parentId: string;
  isObb: boolean;
  packageName: string;
  obbList?: {
    id: string;
    name: string;
  }[];
}

export async function downloadOculusGame(options: downloadRequest) {
  let result = await fetch(`/api/download`, {
    method: "POST",
    body: JSON.stringify(options),
  });

  if (result.status == 403) {
    let json = await result.json();
    throw new Error(json.msg);
  }

  return await result.json();
}

/**
 * Cancel a download
 * @param backupName the name from the download progress object
 */
export async function cancelFileDownload(backupName: string) {
  let params = new URLSearchParams();
  params.append("name", backupName);
  await fetch(`/api/canceldownload?${params}`, {
    method: "POST",
  });
  return;
}

/**
 * Cancel a download from oculus
 * @param unique id of the oculus game
 */
export async function cancelGameDownload(id: string) {
  let params = new URLSearchParams();
  params.append("id", id);
  await fetch(`/api/cancelgamedownload?${params}`, {
    method: "POST",
  });
  return;
}
