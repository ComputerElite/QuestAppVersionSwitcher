import toast from "solid-toast";
import { config } from "../../store";
import { IsOnQuest } from "../../util";
import { isPackageInstalled, uninstallPackage } from "../../api/android";
import { createSignal, onCleanup, onMount, Show } from "solid-js";
import { CustomModal } from "../CustomModal";
import { MediumText } from "../../styles/TextStyles";
import RunButton from "../../components/Buttons/RunButton";
import { FaSolidTrash } from "solid-icons/fa";
import { checkIfGameIsInstalled } from "./utils";

async function uninstallGame() {
  let currentGame = config()?.currentApp;

  if (!currentGame)
    return toast.error(
      "No game selected! Open Change App modal and select a game.",
    );

  if (!IsOnQuest()) {
    toast("Uninstall dialog is open on quest itself!");
  }

  if (!(await isPackageInstalled(currentGame))) {
    return toast.error(
      "Game is not installed, install the game to delete it lol!",
    );
  }

  await uninstallPackage(currentGame);
}

/**
 * Here we ask to uninstall the game
 */
export function UninstallStep(props: {
  next: () => void;
  /* When the modal is closed */
  onClose?: () => void;
}) {
  const [done, setDone] = createSignal(false);
  const [error, setError] = createSignal(false);
  const [inProgress, setInProgress] = createSignal(false);
  const [isInstalled, setIsInstalled] = createSignal(true);

  const timer: NodeJS.Timeout = setInterval(async () => {
    if (!inProgress()) return;
    if (!isInstalled()) return;

    let installed = await checkIfGameIsInstalled();

    // If the gaME IS UNINSTALLED
    if (!installed) {
      setIsInstalled(false);
      setDone(true);
      setInProgress(false);
      clearTimeout(timer);
      toast.success("Game uninstalled successfully");
    }
  }, 400);

  // Skip this step if the game is not installed already
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
      title={"Uninstall the game"}
      open
      onClose={props.onClose}
      buttons={
        <>
          <Show when={done() && !error() && !isInstalled()}>
            <RunButton
              text="Next step"
              variant="success"
              onClick={() => {
                props.next();
              }}
            />
          </Show>
          <Show when={!done() && !error() && isInstalled()}>
            <RunButton
              text="Uninstall"
              icon={<FaSolidTrash />}
              variant="error"
              onClick={uninstallGame}
            />
          </Show>
        </>
      }
    >
      <Show when={isInstalled()}>
        <MediumText>
          To install patched version of the game, you need to uninstall the
          original game first.
        </MediumText>
        <MediumText>
          Press uninstall and confirm the uninstallation on the quest. After
          that, click on the button below to go to the next step.
        </MediumText>
        {/* TODO: Tell the user more info? */}
      </Show>

      <Show when={!isInstalled()}>
        <MediumText>
          The game is successfully uninstalled, click on the button below to go
          to the next step.
        </MediumText>
      </Show>
    </CustomModal>
  );
}
