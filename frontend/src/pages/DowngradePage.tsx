import {
  For,
  Show,
  createEffect,
  createMemo,
  createResource,
  createSignal,
  on,
  onMount,
} from "solid-js";
import { IsOnQuest, RemoveVersionUnderscore, Sleep } from "../util";
import "./DowngradePage.scss";
import { Title } from "@solidjs/meta";
import PageLayout from "../Layouts/PageLayout";
import { MediumText, TitleText } from "../styles/TextStyles";
import { changeManagedApp, proxyFetch } from "../api/app";
import {
  IOculusDBApplication,
  IOculusDBGameVersion,
  OculusDBGetGame,
  OculusDBSearchGame,
} from "../api/oculusdb";
import {
  config,
  currentApplication,
  moddingStatus,
  refetchModdingStatus,
  refetchSettings,
} from "../store";
import toast from "solid-toast";
import {
  Box,
  FormControlLabel,
  List,
  ListItem,
  Switch,
  TextField,
  Typography,
  styled,
} from "@suid/material";
import RunButton from "../components/Buttons/RunButton";
import DownloadRounded from "@suid/icons-material/DownloadRounded";
import { format } from "date-fns";
import { CircularProgress } from "@suid/material";
import { downloadOculusGame } from "../api/downloads";
import { CustomModal } from "../modals/CustomModal";
import { FiRotateCcw } from "solid-icons/fi";
import { FaSolidArrowsLeftRight, FaSolidMagnifyingGlass } from "solid-icons/fa";
import { IoSwapHorizontal, IoSwapVerticalSharp } from "solid-icons/io";
import { debounce } from "lodash";
import { GetBeatsaberModdableVersions } from "../state/mods";

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
  const [gameVersions, setGameVersions] = createSignal<IOculusDBGameVersion[]>(
    [],
  );

  const [gameInfo, setGameInfo] = createSignal<
    IOculusDBApplication | undefined
  >();

  const [showBeta, setShowBeta] = createSignal(false);

  const [loading, setLoading] = createSignal(true);

  const [beatSaberCores, setBeatSaberCores] = createSignal<string[]>([]);

  const [SwitchGameModalOpen, setSwitchGameModalOpen] = createSignal(false);

  createEffect(
    on(currentApplication, async () => {
      setLoading(true);

      // Wait for currentAppName to be set
      if (!config()?.currentAppName) {
        await Sleep(1000);
      }
      if (!config()?.currentAppName) {
        toast.error("No game selected");
        return;
      }

      // Search for the game on OculusDB
      let results = await OculusDBSearchGame(config()!.currentAppName!, test);
      if (results.length == 0) {
        toast.error("Game not found on OculusDB");
        return;
      }

      // Filter out incorrect apps by package name
      results = results.filter(
        (result) => result.packageName == currentApplication(),
      );

      if (results.length == 0) {
        toast.error("Game not found on OculusDB");
        return;
      }
      if (results.length > 1) {
        toast.error("Multiple games found on OculusDB");
        return;
      }

      // Get the first result
      let result = results[0];

      let gameData = await OculusDBGetGame(result.id, test);

      setGameVersions(gameData.versions);
      setGameInfo(gameData.applications[0]);

      // Get beat saber cores
      setBeatSaberCores(await GetBeatsaberModdableVersions());

      setLoading(false);
    }),
  );

  function versionToDisplayVersion(
    version: IOculusDBGameVersion,
    beatSaberCores: string[],
  ): IDisplayVersion {
    let cleanVersion = showBeta()
      ? version.version
      : RemoveVersionUnderscore(version.version);
    let channelsMap = version.binary_release_channels.nodes
      .map((x) => x.channel_name)
      .sort((a, b) => (a == "LIVE" ? 1 : -1));
    let obbList = version.obbList
      ? version.obbList.map((i) => ({ id: i.id, name: i.file_name }))
      : [];

    return {
      releaseChannels: channelsMap,
      gameName: version.parentApplication.displayName,
      gameId: version.parentApplication.id,
      id: version.id,
      live: version.binary_release_channels.nodes
        .map((x) => x.channel_name)
        .includes("LIVE"),
      version: version.version,
      versionPretty: cleanVersion,
      versionCode: version.versionCode,
      date: format(new Date(version.created_date * 1000), "yyyy-MM-dd"),
      downloadable: version.downloadable,
      hasCores: beatSaberCores.includes(version.version),
      isInstalled: moddingStatus()?.version == version.version,
      isLive: channelsMap[channelsMap.length - 1] == "LIVE",
      obbList: obbList,
    };
  }

  const versions = createMemo<IDisplayVersion[]>(() => {
    let versions = gameVersions();
    const showBetaVersions = showBeta();
    let DisplayVersions = [];

    for (const version of versions) {
      if (!version.downloadable) continue;
      if (showBetaVersions) {
        DisplayVersions.push(
          versionToDisplayVersion(version, beatSaberCores()),
        );
        continue;
      } else {
        let isLive = version.binary_release_channels.nodes
          .map((x) => x.channel_name)
          .includes("LIVE");
        if (isLive) {
          DisplayVersions.push(
            versionToDisplayVersion(version, beatSaberCores()),
          );
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

      <div class="flex flex-row items-center">
        <div class="flex-grow flex-wrap">
          <TitleText class="flex gap-1 flex-wrap">
            <div>Downgrade</div>{" "}
            <div class="text-accent">
              {config()?.currentAppName ?? "Unknown"}{" "}
            </div>
          </TitleText>
        </div>

        <div class="flex gap-3 items-center">
          <span class="text-xs text-accent">Not your game? try </span>
          <RunButton
            icon={<IoSwapHorizontal />}
            text="Other game"
            variant="success"
            onClick={() => setSwitchGameModalOpen(true)}
          />
        </div>
      </div>

      <Show when={loading()}>
        <div class="flex justify-center py-20">
          <CircularProgress color="secondary" />
        </div>
      </Show>
      <Show when={!loading()}>
        <FormControlLabel
          sx={{
            pt: 1,
          }}
          control={
            <Switch
              checked={showBeta()}
              onChange={(e, value) => {
                setShowBeta(value);
              }}
            />
          }
          label="Show beta versions"
        />
        <Show when={LatestModdableVersion()}>
          <div class="flex gap-1 flex-col mb-4 mt-3">
            <TitleText>Recommended</TitleText>
            <Show when={LatestModdableVersion()}>
              <ModVersionDiv
                version={LatestModdableVersion()!}
                gameinfo={gameInfo()}
              />
            </Show>
          </div>
        </Show>
        <div class="flex gap-1 flex-col">
          <TitleText>Versions</TitleText>
          <For each={versions()}>
            {(version) => {
              return <ModVersionDiv version={version} gameinfo={gameInfo()} />;
            }}
          </For>
        </div>
      </Show>

      <SwitchGameModal
        open={SwitchGameModalOpen()}
        onClose={() => {
          setSwitchGameModalOpen(false);
        }}
      ></SwitchGameModal>
    </PageLayout>
  );
}

function ModVersionDiv(props: {
  version: IDisplayVersion;
  gameinfo?: IOculusDBApplication;
}) {
  return (
    <div class="bg-[#111827] relative rounded-md shadow-sm flex p-4 ">
      <div class="flex-grow">
        <div class="flex items-baseline">
          <div>{RemoveVersionUnderscore(props.version.version)}</div>
          <div class="ml-2 text-sm text-accent gap-2 flex">
            <Show when={props.version.hasCores}>
              <div>Moddable</div>
            </Show>
            <Show when={props.version.isInstalled}>
              <div>Installed</div>
            </Show>
            <Show when={!props.version.isLive}>
              <div>Beta</div>
            </Show>
          </div>
        </div>
        <div class="text-xs text-slate-400">{props.version.date}</div>
      </div>
      <div class="flex align-middle items-center justify-center h-f">
        <Show when={props.version.downloadable}>
          <DownloadButton version={props.version} gameinfo={props.gameinfo} />
        </Show>
      </div>
      <div class="flex gap-1 absolute right-0 top-0 text-xs flex-nowrap flex-row flex-shrink-0">
        <For each={props.version.releaseChannels}>
          {(channel) => {
            if (channel == "LIVE")
              return <div class="text-accent">{channel}</div>;
            return <div class="text-gray-300">{channel}</div>;
          }}
        </For>
      </div>
    </div>
  );
}

function DownloadButton(props: {
  version: IDisplayVersion;
  gameinfo?: IOculusDBApplication;
}) {
  return (
    <RunButton
      icon={<DownloadRounded />}
      text="Download"
      variant="success"
      onClick={async () => {
        toast.success("Downloading...");

        let ass = {
          app: props.gameinfo!.displayName,
          parentId: props.version.gameId,
          version: props.version.version,
          binaryId: props.version.id,
          isObb: false,
          packageName: props.gameinfo!.packageName,
          password: "",
          obbList: props.version.obbList,
        };
        try {
          await downloadOculusGame(ass);
        } catch (e) {
          toast.error((e as unknown as Error).message);
        }
      }}
    />
  );
}

function IsHeadsetAndroid(h: number) {
  if (h == 0 || h == 5) return false;
  return true;
}

function SwitchGameModal(props: { open: boolean; onClose: () => void }) {
  const [query, setQuery] = createSignal("");

  // Selected game id
  const [selected, setSelected] = createSignal("");

  const [results, { refetch: refetchResults }] = createResource<
    IOculusDBApplication[]
  >(async () => {
    if (query() == "") {
      return [];
    }
    let result = await OculusDBSearchGame(query(), test);
    return result;
  });

  let debouncedRefetch = debounce(refetchResults, 400);

  async function selectGame() {
    let game = results()?.find((x) => x.id == selected());
    if (!game) {
      toast.error("Whaaa, tell frozen that not having a game here is possible");
      return;
    }

    await changeManagedApp(game.packageName, game.appName);

    await refetchSettings();
    await refetchModdingStatus();

    toast.success("Game is updated");
    props.onClose();
  }

  return (
    <CustomModal
      open={props.open}
      title="Select your fav game"
      onClose={props.onClose}
    >
      <div class="flex gap-0 items-center mt-1">
        <SearchInput
          placeholder="Type something.."
          size="small"
          variant="outlined"
          color="primary"
          name="search"
          class="w-full"
          onChange={(_, value) => {
            if (query() != value) {
              debouncedRefetch();
            }
            setQuery(value);
          }}
          value={query()}
        />
      </div>

      <Box sx={{ mt: 0, overflowX: "auto" }}>
        <List
          sx={{
            maxHeight: "50vh",
            minHeight: "200px",
            minWidth: "200px",
          }}
        >
          <Show when={results.loading}>
            <div class="flex justify-center py-20">
              <CircularProgress color="secondary" />
            </div>
          </Show>
          <Show when={!results.error && !results.loading}>
            <For
              each={results()}
              fallback={
                <ListItem
                  sx={{
                    cursor: "pointer",
                    display: "flex",
                    flexDirection: "column",
                    alignItems: "flex-start",
                    backgroundColor: "#1F2937",
                  }}
                >
                  Nothing found
                </ListItem>
              }
            >
              {(app) => (
                <ListItem
                  sx={{
                    cursor: "pointer",
                    display: "flex",
                    flexDirection: "column",
                    alignItems: "flex-start",
                    backgroundColor:
                      selected() == app.id ? "#2e3847" : "#1F2937",
                  }}
                  onClick={() => setSelected(app.id)}
                >
                  <Typography component={"div"} sx={{ color: "white" }}>
                    {app.appName}
                  </Typography>
                  <Typography
                    class="text-accent"
                    component={"div"}
                    fontSize={"0.9em"}
                  >
                    {app.packageName}
                  </Typography>
                </ListItem>
              )}
            </For>
          </Show>
        </List>
      </Box>
      <Box sx={{ flexGrow: 1, display: "flex", justifyContent: "end", gap: 2 }}>
        <RunButton text="Cancel" onClick={props.onClose} />
        <RunButton
          text="Select the game"
          variant="success"
          type="submit"
          disabled={
            selected() == "" ||
            results.loading ||
            results.error ||
            results()?.find((x) => x.id == selected()) == undefined
          }
          onClick={() => selectGame()}
        />
      </Box>
    </CustomModal>
  );
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

export const SearchInput = styled(TextField)({
  borderRadius: "222px",
  outline: "none",

  ".MuiInputBase-root": {
    borderRadius: "6px",

    outline: "none",
    ".MuiOutlinedInput-notchedOutline": {
      "&:hover": {
        border: "1px solid #121827 !important",
        outline: "none",
      },
      border: "1px solid #121827 ",
      // borderRight: "none",
      outline: "none",
    },
    "&:hover .MuiOutlinedInput-notchedOutline": {
      border: "1px solid #121827",
      outline: "none",
    },
    " input:focus-visible": {
      border: "0px solid #121827",
      outline: "none",
    },
  },
});
