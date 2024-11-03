import { createSignal, For, onCleanup, Show } from "solid-js";
import { PatchingProgressData } from "../../state/eventBus";
import { currentApplication, refetchModdingStatus } from "../../store";
import toast from "solid-toast";
import { patchCurrentApp } from "../../api/patching";
import { CustomModal } from "../CustomModal";
import PlayArrowRounded from "@suid/icons-material/PlayArrowRounded";
import RunButton from "../../components/Buttons/RunButton";
import { FirePatch } from "../../assets/Icons";
import { MediumText } from "../../styles/TextStyles";
import { Box, LinearProgress } from "@suid/material";

/**
 * Hete we patch the game and listen for patching events
 */
export function PatchingStep(props: {
  next: () => void;
  onClose?: () => void;
  setBackupName: (backupName: string) => void;
}) {
  const [progress, setProgress] = createSignal(0);

  // TODO: Check for free space before patching

  const [log, setLog] = createSignal<PatchingProgressData[]>([]);

  let logElement: HTMLPreElement | undefined;

  const [done, setDone] = createSignal(false);
  const [error, setError] = createSignal(false);
  const [inProgress, setInProgress] = createSignal(false);

  // Update log when a new event is received
  function onPatchProgress(e: CustomEvent) {
    let data = e.detail as PatchingProgressData;

    // find previous operation
    let prevOperation = log().find(
      (l) => l.currentOperation === data.currentOperation,
    );

    if (prevOperation) {
      // if the operation is the same, replace it
      setLog((old) =>
        old.map((l) => {
          if (l.currentOperation === data.currentOperation) {
            return data;
          }
          return l;
        }),
      );
    } else {
      // if the operation is not the same, add it
      setLog((old) => [...old, data]);
    }

    logElement?.scrollTo(0, logElement.scrollHeight);

    setProgress(data.progress * 100);

    if (data.done) {
      if (data.error) {
        setError(true);
        toast.error("Failed to patch the game");
      } else {
        setDone(true);
        props.setBackupName(data.backupName);
        toast.success("Game patched successfully");
      }

      // Remove the listener
      // @ts-ignore
      BackendEvents.removeEventListener("patch-progress", onPatchProgress);
    }
  }

  async function startPatching() {
    setInProgress(true);
    try {
      let result = await patchCurrentApp();
      if (!result) {
        toast.error("Failed to patch the game");
        return;
      }
      await refetchModdingStatus();
      setInProgress(true);
    } catch (e) {
      console.error(e);
      toast.error("Failed to patch game");
      setInProgress(false);
      setError(true);
    }

    // @ts-ignore
    BackendEvents.addEventListener("patch-progress", onPatchProgress);
  }

  onCleanup(() => {
    // @ts-ignore
    BackendEvents.removeEventListener("patch-progress", onPatchProgress);
  });

  return (
    <CustomModal
      title={"Patching"}
      open
      onClose={props.onClose}
      buttons={
        <>
          {/* If success, allow to go to next step (Uninstalling) */}
          <Show when={done() && !error()}>
            <RunButton
              text="Next step"
              icon={<PlayArrowRounded />}
              variant="success"
              onClick={() => {
                props.next();
              }}
            />
          </Show>
          {/* If just started show the patch button */}
          <Show when={!done() && !error() && !inProgress()}>
            <RunButton
              text="Patch"
              icon={<FirePatch />}
              variant="success"
              onClick={startPatching}
            />
          </Show>
        </>
      }
    >
      <Show when={!inProgress() && !done()}>
        <MediumText>
          To start the patching process, click on the button below.
        </MediumText>
      </Show>

      <Show when={inProgress()}>
        <Box sx={{ width: "100%" }}>
          <LinearProgress variant="determinate" value={progress()} />
        </Box>
        <pre
          ref={logElement}
          style={{
            background: "black",
            color: "white",
            padding: "10px",
            "border-radius": "0px",
            "min-width": "400px",
            "max-width": "100vw",
            height: "300px",
            "overflow-y": "auto",
            "font-size": "12px",
          }}
        >
          <For each={log()}>{(line) => <LogLine line={line} />}</For>
        </pre>
      </Show>
      {/* If done succesfully */}
      <Show when={!inProgress() && done() && !error()}>
        <MediumText>
          The patching is completed successfully. To continue, click on the
          button below.
        </MediumText>
      </Show>
    </CustomModal>
  );
}

function LogLine({ line }: { line: PatchingProgressData }) {
  return (
    <div>
      ({line.doneOperations - 1}/{line.totalOperations}) {line.currentOperation}
      {line.done ? "OK" : "..."}{" "}
    </div>
  );
}
