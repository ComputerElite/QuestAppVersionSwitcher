import { For, Show, createMemo, createSignal, onMount } from "solid-js";
import { IsOnQuest, RemoveVersionUnderscore, Sleep } from "../util";
import './DowngradePage.scss'
import { Title } from "@solidjs/meta";
import PageLayout from "../Layouts/PageLayout";
import { TitleText } from "../styles/TextStyles";
import { proxyFetch } from "../api/app";
import { IOculusDBGameVersion, OculusDBGetGame, OculusDBSearchGame } from "../api/oculusdb";
import { config, moddingStatus } from "../store";
import toast from "solid-toast";
import { FormControlLabel, Switch } from "@suid/material";
import RunButton from "../components/Buttons/RunButton";
import DownloadRounded from "@suid/icons-material/DownloadRounded"
import { getCoreModsList } from "./BeatSaber/GetBeatSaberMods";
import { format } from "date-fns";
import { CircularProgress } from "@suid/material"
import { downloadOculusGame } from "../api/downloads";

const test = false;

interface IDisplayVersion {
  releaseChannels: string[];
  id: string;
  live: boolean;
  gameName: string;
  gameId: string;
  version: string;
  versionPretty: string;
  versionCode: number;
  date: string;
  downloadable: boolean;
  hasCores: boolean;
  isInstalled: boolean;
  isLive: boolean;
  obbList?: {
    id: string;
    name: string;
  }[];
}


export default function DowngradePage() {
  const [gameVersions, setGameVersions] = createSignal<IOculusDBGameVersion[]>([]);

  const [showBeta, setShowBeta] = createSignal(false);

  const [loading, setLoading] = createSignal(true);

  const [beatSaberCores, setBeatSaberCores] = createSignal<string[]>([]);

  onMount(async () => {
    setLoading(true);

    if (!config()?.currentAppName) { await Sleep(1000) }

    if (!config()?.currentAppName) { toast.error("No game selected"); return; }
    let results = await OculusDBSearchGame(config()!.currentAppName!, test);
    if (results.length == 0) { toast.error("Game not found on OculusDB"); return; }

    // Filter out demo
    results = results.filter((result) => !result.appName.includes("Demo"));

    if (results.length == 0) { toast.error("Game not found on OculusDB"); return; }
    if (results.length > 1) { toast.error("Multiple games found on OculusDB"); return; }

    // Get the first result
    let result = results[0];

    let gameData = await OculusDBGetGame(result.id, test);

    setGameVersions(gameData.versions);

    let coreModsBeatSaber = await getCoreModsList();
    let versionsWithCores = Object.keys(coreModsBeatSaber);
    setBeatSaberCores(versionsWithCores);

    setLoading(false);
  });



  function versionToDisplayVersion(version: IOculusDBGameVersion, beatSaberCores: string[]): IDisplayVersion {
    let cleanVersion = (showBeta()) ? version.version : RemoveVersionUnderscore(version.version);
    let channelsMap = version.binary_release_channels.nodes.map(x => x.channel_name).sort((a, b) => a == "LIVE" ? 1 : -1);
    let obbList = version.obbList? version.obbList.map((i)=> ({id: i.id, "name": i.file_name})) :[];

    
    return {
      releaseChannels: channelsMap,
      gameName: version.parentApplication.displayName,
      gameId: version.parentApplication.id,
      id: version.id,
      live: version.binary_release_channels.nodes.map(x => x.channel_name).includes("LIVE"),
      version: version.version,
      versionPretty: cleanVersion,
      versionCode: version.versionCode,
      date: format(new Date(version.created_date * 1000), "yyyy-MM-dd"),
      downloadable: version.downloadable,
      hasCores: beatSaberCores.includes(version.version),
      isInstalled: moddingStatus()?.version == version.version,
      isLive: channelsMap[channelsMap.length - 1] == "LIVE",
      obbList: obbList
    }
  }

  const versions = createMemo<IDisplayVersion[]>(() => {
    let versions = gameVersions();

    let DisplayVersions = [];

    for (const version of versions) {
      if (!version.downloadable) continue;
      if (showBeta()) {
        DisplayVersions.push(versionToDisplayVersion(version, beatSaberCores()));
        continue;
      } else {
        let isLive = version.binary_release_channels.nodes.map(x => x.channel_name).includes("LIVE");
        if (isLive) {
          DisplayVersions.push(versionToDisplayVersion(version, beatSaberCores()));
          continue;
        }
      }

    }
    return DisplayVersions;
  });

  const LatestModdableVersion = createMemo<IDisplayVersion | undefined>(() => {
    let coresList = beatSaberCores();
    if (coresList.length == 0) return undefined;

    for (const version of versions()) {
      if (version.hasCores) return version;
    }
  });

  return (
    <PageLayout hasOffset={true}>
      <Title>Downgrade</Title>
      
      <Show when={loading()}>
        <div class="flex justify-center py-20">
          <CircularProgress color="secondary" />
        </div>
      </Show>
      <Show when={!loading()}>
        <TitleText>Downgrade {config()?.currentAppName ?? "Unknown"} to a previous version</TitleText>
        <FormControlLabel sx={{
          pt: 1
        }}
          control={<Switch checked={showBeta()} onChange={(e, value) => {
            setShowBeta(value);
          }} />}
          label="Show beta versions"
        />
        <Show when={LatestModdableVersion()} >
          <div class="flex gap-1 flex-col mb-4 mt-3">
            <TitleText>Latest Moddable version</TitleText>
            <Show when={LatestModdableVersion()}>
              <ModVersionDiv version={LatestModdableVersion()!} />
            </Show>
          </div>
        </Show>
        <div class="flex gap-1 flex-col">
          <TitleText>Versions</TitleText>
          <For each={versions()}>
            {(version) => {
              return <ModVersionDiv version={version} />
            }}
          </For>
        </div>

      </Show>

    </PageLayout>

  )
}

function ModVersionDiv(props: { version: IDisplayVersion }) {
  return (
    <div class="bg-[#111827] relative rounded-md shadow-sm flex p-4 ">
      <div class="flex-grow">
        <div class="version__header__version">
          <span>{RemoveVersionUnderscore(props.version.version)}</span>
          <Show when={props.version.hasCores}>
            <span class="ml-2 text-sm text-accent">Moddable</span>
          </Show>
          <Show when={props.version.isInstalled}>
            <span class="ml-2 text-sm text-accent">Installed</span>
          </Show>
          <Show when={!props.version.isLive}>
            <span class="ml-2 text-sm text-accent">Beta</span>
          </Show>
        </div>
        <div class="version__header__date">{props.version.date}</div>
      </div>
      <div class="flex align-middle items-center justify-center h-f">
        <Show when={props.version.downloadable}>
          <DownloadButton version={props.version} />
        </Show>
      </div>
      <div class="flex gap-1 absolute right-0 top-0 text-xs flex-nowrap flex-row flex-shrink-0">
        <For each={props.version.releaseChannels}>
          {(channel) => {
            if (channel == "LIVE") return <div class="text-accent">{channel}</div>
            return <div class="text-gray-300">{channel}</div>
          }}
        </For>
      </div>
    </div>);
}

function DownloadButton(props: { version: IDisplayVersion }) {
  
  
  return (
    <RunButton icon={<DownloadRounded />} text="Download" variant="success" onClick={() => {
      toast.success("Downloading...");

      downloadOculusGame({
        app: props.version.gameName,
        parentId: props.version.gameId,
        version: props.version.version,
        binaryId: props.version.id,
        isObb: false,
        packageName: "",
        password: "",
        obbList: props.version.obbList
      });
    }} />
  )
}


function IsHeadsetAndroid(h: number) {
  if (h == 0 || h == 5) return false;
  return true
}


/*
function GetDownloadButtonVersion(downloadable, id, hmd, parentApplication, version, isObb = false, obbIds = "", obbNames = "") {
  if(IsHeadsetAndroid(hmd)) {
      if(localStorage.isOculusDowngrader) {
          return `<input type="button" value="Download${downloadable ? '"' : ' (Developer only)" class="red"'} onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) AndroidDownloadPopUp('${parentApplication.id}','${id}', '${hmd}')" oncontextmenu="ContextMenuEnabled(event, this)">`
      }
      return `<input type="button" value="Download${downloadable ? '"' : ' (Developer only)" class="red"'} onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) AndroidDownload('${id}', '${parentApplication.id}', '${parentApplication.displayName.replace("'", "\\'")}', '${version}', ${isObb}, ${obbIds == null ? "null" : `'${obbIds}'`}, ${obbNames == null ? "null" : `'${obbNames}'`})" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy download url" cmov-0="Copy(GetDownloadLink('${id}'))" cmon-1="Show Oculus Downgrader code" cmov-1="AndroidDownloadPopUp('${parentApplication.id}','${id}', '${hmd}')">`
  }
  return `<input type="button" value="Download${downloadable ? '"' : ' (Developer only)" class="red"'} onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) RiftDownloadPopUp('${parentApplication.id}','${id}')" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Show Oculus Downgrader code" cmov-0="RiftDownloadPopUp('${parentApplication.id}','${id}')">`
}


function AndroidDownload(id, parentApplicationId,parentApplicationName, version, isObb = false, obbIds = "", obbNames = "") {
  var obbs = []
  if(obbIds) {
      var obbIdsSplit = obbIds.split(",")
      var obbNamesSplit = obbNames.split("/")
      for(let i = 0; i < obbIds.length; i++) {
          obbs.push({
              id: obbIdsSplit[i],
              name: obbNamesSplit[i]
          })
      }
  }
  data = {
      type: "Download",
      binaryId: id,
      parentId: parentApplicationId,
      parentName: parentApplicationName,
      version: version,
      isObb: isObb,
      obbList: obbs,
      downloadLink: GetDownloadLink(id)
  }
  if(sendToParent) {
      fetch(`/api/v1/id/${parentApplicationId}`).then(res => res.json().then(res => {
          data.packageName = res.packageName
          SendDataToParent(JSON.stringify(data))
      }))
      return
  }

  // Not in iframe which supports downloads
  if(localStorage.fuckpopups && !jokeconfig.dialupdownload) {
      DownloadID(id)
      if(obbs && obbs.length > 0){
          ObbDownloadPopUp()
      }
      if(isObb && !sendToParent) ObbInfoPopup()
  } else {
      PopUp(`
      <div>
          To download games you must be logged in on <a href="https://oculus.com/experiences/quest">https://oculus.com/experiences/quest</a>. If you aren't logged in you won't be able to download games.
          <br>
          <a onclick="localStorage.fuckpopups = 'yummy, spaghetti'; window.open(GetDownloadLink('${id}')); ClosePopUp();"><i style="cursor: pointer;">Don't show warning again</i></a>
          <div class="textbox" id="downloadTextBox"></div>
          <div>
              <input type="button" value="Log in" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) window.open('https://oculus.com/experiences/quest', )">
              <input type="button" value="Download" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) { OpenDownloadWithJokes('${id}', ${obbs && obbs.length > 0}); ${isObb && !sendToParent ? `ObbInfoPopup();` : ``}}">
          </div>
      </div>
  `)
  }
 
}

*/
