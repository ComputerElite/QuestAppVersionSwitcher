import { Title } from "@solidjs/meta";
import "./ToolsPage.scss";
import {
  changeManagedApp,
  changePort,
  exitApp,
  setOculusToken,
} from "../api/app";
import {
  uninstallPackage,
  isPackageInstalled,
  launchCurrentApp,
  hasManageStorageAccess,
  grantManageStorageAccess,
  gotAccessToAppAndroidFolders,
  grantAccessToAppAndroidFolders,
} from "../api/android";
import { showChangeGameModal } from "../modals/ChangeGameModal";
import PageLayout from "../Layouts/PageLayout";
import { Box, TextField, Typography } from "@suid/material";
import RunButton from "../components/Buttons/RunButton";
import { DeleteIcon, UploadRounded } from "../assets/Icons";
import PlayArrowRounded from "@suid/icons-material/PlayArrowRounded";
import { appInfo, config, moddingStatus, refetchSettings } from "../store";
import { IsOnQuest, OpenOculusAuthLink, Sleep, ValidateToken } from "../util";
import toast from "solid-toast";
import { Show, createEffect, createSignal, on } from "solid-js";
import {
  FaBrandsMeta,
  FaSolidCircleArrowLeft,
  FaSolidKey,
} from "solid-icons/fa";
import { FiExternalLink, FiKey, FiRefreshCcw, FiTrash2 } from "solid-icons/fi";
import { DeleteAllMods } from "../api/mods";
import { showConfirmModal } from "../modals/ConfirmModal";
import { IoExitOutline } from "solid-icons/io";
import { A } from "@solidjs/router";

export const OptionHeader = (props: { children: any }) => {
  return (
    <Typography
      sx={{
        fontFamily: "Roboto",
        fontStyle: "normal",
        fontWeight: 700,
        fontSize: "18px",
        lineHeight: "21px",
        marginBottom: 1,
      }}
      variant="h4"
    >
      {props.children}
    </Typography>
  );
};

export default function ToolsPage() {
  // Local state
  const [port, setPort] = createSignal<number | null>(
    config()?.serverPort ?? null,
  );
  const [showTokenPrompt, setShowTokenPrompt] = createSignal(false);

  // React to port change in config
  createEffect(
    on(config, (config) => {
      if (config?.serverPort == port()) return;
      if (config?.serverPort == null) return;
      console.log("Port changed in config, updating local state");
      setPort(config.serverPort);
    }),
  );

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

  async function removeAllModsClick() {
    let sure = await showConfirmModal({
      title: "Delete all mods?",
      cancelText: "Cancel",
      okText: "Delete",
      message:
        "Are you sure you want to delete all mods? This action is irreversible!",
    });
    if (!sure) return;

    await DeleteAllMods();
    toast.success("All mods deleted!");
  }

  async function startGame() {
    let currentGame = config()?.currentApp;

    if (!currentGame)
      return toast.error(
        "No game selected! Open Change App modal and select a game.",
      );

    if (!(await isPackageInstalled(currentGame))) {
      return toast.error("Game is not installed, install the game to run it!");
    }

    if (!IsOnQuest()) {
      toast.success("Game is opened on quest");
    }

    await launchCurrentApp();
  }

  async function getPermissionsToGameFolderClick() {
    let currentGame = config()?.currentApp;
    if (!currentGame) {
      return toast.error(
        "No game selected! Open Change App modal and select a game.",
      );
    }

    if (await gotAccessToAppAndroidFolders(currentGame)) {
      return toast.error("Permissions are already granted!");
    }

    await grantAccessToAppAndroidFolders(currentGame);
  }

  async function allowManageStorageClick() {
    let currentGame = config()?.currentApp;
    if (!currentGame) {
      return toast.error(
        "No game selected! Open Change App modal and select a game.",
      );
    }

    if (await hasManageStorageAccess(currentGame)) {
      return toast.error("Permission is already granted!");
    }
    grantManageStorageAccess(currentGame);
  }

  async function changePortClick() {
    let requestedPort = port();
    if (!requestedPort) return toast.error("Invalid port!");
    if (requestedPort < 0 || requestedPort > 65535)
      return toast.error("Invalid port!");
    if (requestedPort == config()?.serverPort)
      return toast.error("Port is already set to this value!");
    if (requestedPort == 0) return toast.error("Port cannot be 0!");
    if (requestedPort < 1024)
      return toast.error("Port cannot be lower than 1024!");
    try {
      await changePort(requestedPort);
      toast.success(
        `Port changed to ${requestedPort}, restart the app to apply changes!`,
      );

      // Refresh application state to keep the ui working
      await refetchSettings();
    } catch (e) {
      // @ts-ignore
      toast.error(`Failed to change port! ${e?.message ?? ""}`);
    }
  }

  return (
    <PageLayout>
      <div class="contentItem toolsPage">
        <Title>Tools</Title>
        <Box
          sx={{
            display: "flex",
            width: "100%",
            gap: 1,
            flexWrap: "wrap",
            justifyContent: "space-between",
            marginBottom: 1,
          }}
        >
          <Box
            sx={{
              display: "flex",
              gap: 2,
              alignItems: "center",
            }}
          >
            <RunButton
              text="Run Game"
              variant="success"
              icon={<PlayArrowRounded />}
              onClick={startGame}
            />

            <Show when={config()?.currentApp}>
              <RunButton
                text="Reload mods"
                icon={<FiRefreshCcw />}
                onClick={async () => {
                  await changeManagedApp(config()?.currentApp ?? "");
                  await refetchSettings();
                  toast.success("Finished reloading mods!");
                }}
              />
              <RunButton
                text="Delete all mods"
                variant="error"
                icon={<FiTrash2 />}
                onClick={removeAllModsClick}
              />
              <RunButton
                text="Uninstall game"
                disabled={!moddingStatus()?.isInstalled}
                variant="error"
                icon={<FiTrash2 />}
                onClick={uninstallGame}
              />
            </Show>
          </Box>
        </Box>
        <div
          class="text-accent"
          style={{
            "font-family": "Roboto",
            "font-style": "normal",
            "font-weight": "400",
            "font-size": "12px",
            "line-height": "14px",
            display: "flex",
            "margin-bottom": "14px",
          }}
        >
          Wi-Fi IPs (not public):{" "}
          {appInfo()?.browserIPs.join(", ") ?? "Loading"}
        </div>

        <Box sx={{ marginY: 3 }}>
          <OptionHeader>Permissions</OptionHeader>

          <Box
            sx={{
              display: "flex",
              gap: 2,
              alignItems: "center",
            }}
          >
            <RunButton
              text="Give permissions to game folder"
              onClick={getPermissionsToGameFolderClick}
            />
            <RunButton
              text="Allow manage storage permission"
              onClick={allowManageStorageClick}
            />
          </Box>
        </Box>

        <Box sx={{ marginY: 3 }}>
          <OptionHeader>Login to Oculus for downloading</OptionHeader>

          <Typography
            component={"p"}
            sx={{
              fontFamily: "Roboto",
              fontStyle: "normal",
              fontWeight: 400,
              fontSize: "12px",
              lineHeight: "14px",
              display: "flex",
              alignItems: "center",
              color: "#D1D5DB",
            }}
          >
            Log in with your Oculus/Facebook account to downgrade games, browser
            login works on quest only
          </Typography>
          <Box
            sx={{
              display: "flex",
              gap: 2,
              alignItems: "center",
              marginTop: 1,
            }}
          >
            <RunButton
              icon={<FaBrandsMeta />}
              text="Login using meta account (quest only)"
              disabled={!IsOnQuest()}
              onClick={() => OpenOculusAuthLink()}
            />
            <RunButton
              icon={<FaSolidKey />}
              text="Login using a token"
              onClick={() => setShowTokenPrompt(true)}
            />
            <RunButton icon={<IoExitOutline />} text="Logout" />
          </Box>

          <form
            onSubmit={async (e) => {
              e.preventDefault();
              let formData = new FormData(e.currentTarget as HTMLFormElement);
              let token = formData.get("token") as string;
              if (!token) return toast.error("Token is empty!");
              if (token.length < 10) return toast.error("Token is too short!");
              let tokenValid = ValidateToken(token);
              if (tokenValid.isValid) {
                try {
                  // TODO: add password support
                  await setOculusToken(token, "");
                  toast.success("Token set!");
                } catch (e: any) {
                  toast.error(`Failed to set token! ${e?.message ?? ""}`);
                }
              } else {
                toast.error(`Token is invalid! ${tokenValid.message}`);
              }
            }}
          >
            <Box
              sx={{
                display: "flex",
                gap: 0,
                alignItems: "center",
                marginTop: 1,
              }}
            >
              <TextField
                placeholder="Enter your token here"
                type="password"
                sx={{
                  borderRadius: "222px",
                  outline: "none",

                  ".MuiInputBase-root": {
                    borderRadius: "6px 0px 0px 6px",

                    outline: "none",
                    ".MuiOutlinedInput-notchedOutline": {
                      "&:hover": {
                        border: "1px solid #121827 !important",
                        outline: "none",
                      },
                      border: "1px solid #121827 ",
                      borderRight: "none",
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
                }}
                size="small"
                variant="outlined"
                color="primary"
                name="token"
              />

              <RunButton
                type="submit"
                icon={<FaSolidKey />}
                style={{
                  height: "40px",
                  width: "100px",
                  "border-radius": "0px 6px 6px 0px",
                }}
                text="Set token"
              />
            </Box>
            <A
              class="text-sm text-accent underline"
              target={!IsOnQuest() ? "_blank" : ""}
              href="https://computerelite.github.io/tools/Oculus/ObtainTokenNew.html"
            >
              Guide to get your token
            </A>
          </form>
        </Box>

        <Box sx={{ marginY: 3 }}>
          <OptionHeader>Server control</OptionHeader>
          <Box
            sx={{
              display: "flex",
              gap: 0,
              alignItems: "center",
              marginTop: 1,
            }}
          >
            <TextField
              sx={{
                borderRadius: "222px",
                ".MuiInputBase-root": {
                  borderRadius: "6px 0px 0px 6px",
                },
              }}
              size="small"
              variant="outlined"
              color="primary"
              value={port()}
              onChange={(e) => {
                let value = e.currentTarget.value;
                if (!value) return setPort(0);
                if (isNaN(parseInt(value))) return;

                setPort(parseInt(e.currentTarget.value));
              }}
            />

            <RunButton
              style={{
                height: "40px",
                width: "100px",
                "border-radius": "0px 6px 6px 0px",
              }}
              text="Change port"
              variant="success"
              onClick={changePortClick}
            />
          </Box>
        </Box>
      </div>
    </PageLayout>
  );
}
