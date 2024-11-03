import { createSignal, onCleanup, onMount, Show } from "solid-js";
import { checkIfGameIsInstalled, installGame } from "./utils";
import toast from "solid-toast";
import { config } from "../../store";
import { CustomModal } from "../CustomModal";
import RunButton from "../../components/Buttons/RunButton";
import { MediumText, SmallText } from "../../styles/TextStyles";
import { IBackup } from "../../api/backups";

/**
 * Here we ask to install the game
 * @param props
 * @returns
 */
export function InstallStep(props: {
  next: () => void;
  onClose?: () => void;
  /**
   * Selected backup to install (if we want to show additional info)
   */
  selectedBackup?: IBackup;
  /**
   * Name of the backup to install (required)
   */
  backupName: string;
}) {
  const [done, setDone] = createSignal(false);
  const [error, setError] = createSignal(false);
  const [inProgress, setInProgress] = createSignal(false);
  const [isInstalled, setIsInstalled] = createSignal(false);

  const timer: NodeJS.Timeout = setInterval(async () => {
    if (!inProgress()) return;
    if (isInstalled()) return;

    let installed = await checkIfGameIsInstalled();

    if (installed) {
      setIsInstalled(true);
      setDone(true);
      setInProgress(false);
      clearInterval(timer);
      toast.success("Game is installed successfully");
    }
  }, 400);

  onMount(async () => {
    setInProgress(true);

    let currentApp = config()?.currentApp;

    if (!currentApp) {
      toast.error("No game selected");
      return;
    }
  });

  onCleanup(() => {
    if (timer) clearInterval(timer);
  });

  return (
    <CustomModal
      title={"Install the patched game"}
      open
      onClose={props.onClose}
      buttons={
        <>
          {/* If success, allow to go to next step (Uninstalling) */}
          <Show when={done() && !error() && isInstalled()}>
            <RunButton
              text="Next step"
              variant="success"
              onClick={() => {
                props.next();
              }}
            />
          </Show>
          {/* If just started show the patch button */}
          <Show when={!done() && !error() && !isInstalled()}>
            <RunButton
              text="Install"
              variant="success"
              onClick={() => installGame(props.backupName)}
            />
          </Show>
        </>
      }
    >
      <Show when={isInstalled()}>
        <MediumText>
          The game is successfully <span class="text-accent">installed</span>,
          click on the button below to go to the next step.
        </MediumText>
      </Show>

      <Show when={!isInstalled()}>
        <MediumText>
          Install the game on your quest, click on the button below to install
          the game. After that, click on the button below to go to the next
          step.
        </MediumText>
        <SmallText>
          If the game won't install, check your free space on the quest. You
          should have at least 3gb free. If you have enough space, try to
          restart the quest and try again. One more issue could be that you have
          the game badly uninstalled, in this case you need to run adb command
          to uninstall the game using SideQuest.
        </SmallText>
      </Show>
    </CustomModal>
  );
}
