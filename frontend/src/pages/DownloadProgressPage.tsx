import { Title } from "@solidjs/meta";
import PageLayout from "../Layouts/PageLayout";
import {
  For,
  Show,
  createMemo,
  createSignal,
  onCleanup,
  onMount,
} from "solid-js";
import { Box, IconButton } from "@suid/material";
import { A } from "@solidjs/router";
import RunButton from "../components/Buttons/RunButton";
import FastRewindSharp from "@suid/icons-material/FastRewindSharp";
import {
  IAPIDownloadsResponse,
  IDownloadManagerInfo,
  IGameDownloadItem,
  cancelGameDownload,
  getDownloads,
} from "../api/downloads";
import { BackendEvents } from "../state/eventBus";
import { FiTrash, FiX } from "solid-icons/fi";
import { FaSolidCross, FaSolidX } from "solid-icons/fa";
import { showConfirmModal } from "../modals/ConfirmModal";
import toast from "solid-toast";
import { createDeepSignal } from "../util";

export default function DownloadProgressPage() {
  const [gameDownloads, setGameDownloads] = createDeepSignal<
    IGameDownloadItem[]
  >([]);

  function onDownloadProgress(e: any) {
    let response = (e as CustomEvent).detail as IAPIDownloadsResponse;
    setGameDownloads(response.gameDownloads);
    console.log(response);
  }

  onMount(async () => {
    let downloads = await getDownloads();
    setGameDownloads(downloads.gameDownloads);

    BackendEvents.addEventListener("download-progress", onDownloadProgress);
  });

  onCleanup(() => {
    BackendEvents.removeEventListener("download-progress", onDownloadProgress);
  });

  return (
    <PageLayout>
      <div class="contentItem">
        <Title>Downloads</Title>
        <div class="flex flex-col gap-2 shadow-sm">
          <Show when={gameDownloads().length === 0}>
            <Box
              sx={{
                display: "flex",
                justifyContent: "center",
                flexDirection: "column",
                alignItems: "center",
                textAlign: "center",
              }}
            >
              <Box
                sx={{
                  marginBottom: "1rem",
                }}
              >
                No downloads yet, to download a game go to
              </Box>
              <Box>
                <A href="/downgrade/">
                  <RunButton
                    text="Downgrade"
                    icon={<FastRewindSharp />}
                  ></RunButton>
                </A>
              </Box>
            </Box>
          </Show>
          <Show when={gameDownloads().length > 0}>
            <For each={gameDownloads()}>
              {(download) => (
                <GameDownloadItem download={download}></GameDownloadItem>
              )}
            </For>
          </Show>
        </div>
      </div>
    </PageLayout>
  );
}

enum StateOfDownload {
  Downloading,
  Canceled,
  Failed,
  Done,
}

async function cancelAppDL(item: IGameDownloadItem) {
  let sure = await showConfirmModal({
    title: "Cancel download",
    message: "Are you sure you want to cancel this download?",
    cancelText: "No",
    okText: "Yes",
  });

  if (!sure) {
    return;
  }

  await cancelGameDownload(item.id);
  toast("Download canceled");
}

function GameDownloadItem(props: { download: IGameDownloadItem }) {
  console.log(props.download);

  const downloadStatus = createMemo<{
    state: StateOfDownload;
    download: IGameDownloadItem;
    manager?: IDownloadManagerInfo;
    status?: string;
  }>(() => {
    let state = StateOfDownload.Downloading;
    let manager: IDownloadManagerInfo | undefined = undefined;

    let download = props.download;

    if (download.error) {
      state = StateOfDownload.Failed;
    }
    if (download.done) {
      state = StateOfDownload.Done;
    }
    if (download.canceled) {
      state = StateOfDownload.Canceled;
    }

    if (
      state === StateOfDownload.Downloading &&
      download.downloadManagers.length > 0
    ) {
      if (download.downloadManagers.length > 1) {
        console.warn("More than one download manager found, using first one");
      }
      let dlman = download.downloadManagers[0];
      if (dlman) {
        manager = dlman;
      }
    }

    return {
      state: state,
      download: download,
      manager: manager,
      status: download.status,
    };
  });

  return (
    <div class="bg-[#111827] rounded relative overflow-hidden ">
      <Show
        when={
          downloadStatus().state === StateOfDownload.Downloading &&
          downloadStatus().manager
        }
      >
        <div class="absolute left-0 right-0 top-0 h-1">
          <div
            class="h-full bg-accent transition-all"
            style={` width: ${Math.floor(downloadStatus().manager!.percentage * 100)}%`}
          >
            {" "}
          </div>
        </div>
      </Show>
      <div class="p-4 flex flex-row">
        <div class="flex-grow">
          <div class="flex gap-2 items-baseline">
            <div class="text-md">{downloadStatus().download.gameName}</div>
            <div class="text-xs text-accent">
              {downloadStatus().download.version}
            </div>
          </div>
          <div class="text-xs ">
            <Show
              when={
                downloadStatus().state === StateOfDownload.Downloading &&
                downloadStatus().manager
              }
            >
              <span class="text-accent">
                Downloading |{" "}
                {Math.floor(downloadStatus().manager!.percentage * 100)}% |{" "}
                {downloadStatus().manager!.speedString} | ETA:{" "}
                {downloadStatus().manager!.eTAString}
              </span>
              <Show when={downloadStatus().status}>
                <span class="text-gray-300"> | {downloadStatus().status}</span>
              </Show>
            </Show>
            <Show when={downloadStatus().state === StateOfDownload.Canceled}>
              <span class="text-gray-300">Canceled</span>
              <Show when={downloadStatus().status}>
                <span class="text-gray-300"> | {downloadStatus().status}</span>
              </Show>
            </Show>
            <Show when={downloadStatus().state === StateOfDownload.Failed}>
              <span class="text-red-500">Failed</span>
              <Show when={downloadStatus().status}>
                <span class="text-gray-300"> | {downloadStatus().status}</span>
              </Show>
            </Show>
            <Show when={downloadStatus().state === StateOfDownload.Done}>
              <span class="text-accent">Done</span>
            </Show>
          </div>
        </div>
        <div class="flex flex-row gap-2">
          <Show
            when={
              downloadStatus().state === StateOfDownload.Downloading &&
              downloadStatus().manager
            }
          >
            <IconButton
              color="info"
              onClick={() => cancelAppDL(props.download)}
            >
              <FiX />
            </IconButton>
          </Show>
        </div>
      </div>
    </div>
  );
}
