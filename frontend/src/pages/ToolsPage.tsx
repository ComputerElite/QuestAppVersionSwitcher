import { Title } from "@solidjs/meta"
import "./ToolsPage.scss"
import { changePort, exitApp } from "../api/app"
import { uninstallPackage, isPackageInstalled, launchCurrentApp } from "../api/android"
import { showChangeGameModal } from "../modals/ChangeGameModal"
import PageLayout from "../Layouts/PageLayout"
import { Box, TextField, Typography } from "@suid/material"
import RunButton from "../components/Buttons/RunButton"
import { DeleteIcon, UploadRounded } from "../assets/Icons"
import PlayArrowRounded from "@suid/icons-material/PlayArrowRounded"
import { appInfo, config, refetchSettings } from "../store"
import { IsOnQuest, Sleep } from "../util"
import toast from "solid-toast"
import { createEffect, createSignal, on } from "solid-js"

const OptionHeader = (props: { children: any }) => {
  return (
    <Typography sx={{
      fontFamily: 'Roboto',
      fontStyle: 'normal',
      fontWeight: 700,
      fontSize: "18px",
      lineHeight: "21px",
      marginBottom: 1,
    }} variant="h4">{props.children}</Typography>
  )
}

export default function ToolsPage() {
  // Local state
  const [port, setPort] = createSignal<number|null>(config()?.serverPort ?? null)


  // React to port change in config
  createEffect(on(config, (config) => {
    if (config?.serverPort == port()) return
    if (config?.serverPort == null) return
    console.log("Port changed in config, updating local state")
    setPort(config.serverPort);
  }))



  function uninstallGame() {
    let currentGame = config()?.currentApp;

    if (!currentGame) return toast.error("No game selected! Open Change App modal and select a game.")

    if (!IsOnQuest()) {
      toast("Uninstall dialog is open on quest itself!")
    }

    uninstallPackage(currentGame);
  }

  async function startGame() {
    let currentGame = config()?.currentApp;

    if (!currentGame) return toast.error("No game selected! Open Change App modal and select a game.")

    if (!await isPackageInstalled(currentGame)) {
      return toast.error("Game is not installed, install the game to run it!")
    }

    if (!IsOnQuest()) {
      toast.success("Game is opened on quest")
    }

    await launchCurrentApp();
  }

  async function changePortClick() {
    let requestedPort = port()
    if (!requestedPort) return toast.error("Invalid port!")
    if (requestedPort < 0 || requestedPort > 65535) return toast.error("Invalid port!")
    if (requestedPort == config()?.serverPort) return toast.error("Port is already set to this value!")
    if (requestedPort == 0) return toast.error("Port cannot be 0!")
    if (requestedPort < 1024) return toast.error("Port cannot be lower than 1024!")
    try {
      await changePort(requestedPort)
      toast.success(`Port changed to ${requestedPort}, restart the app to apply changes!`)
      
      // Refresh application state to keep the ui working
      await refetchSettings();
    } catch (e) {
      toast.error(`Failed to change port! ${e?.message ?? ""}`)
    }
  }


  return (
    <PageLayout>
      <div class="contentItem toolsPage">
        <Title>Tools</Title>
        <Box sx={{
          display: "flex",
          width: "100%",
          gap: 1,
          flexWrap: "wrap",
          justifyContent: "space-between",
          marginBottom: 1,
        }}>
          <Box sx={{
            display: "flex",
            gap: 2,
            alignItems: "center",
          }}>
            <RunButton text='Run Game' variant="success" icon={<PlayArrowRounded />} onClick={startGame} />
            <RunButton text='Uninstall game' icon={<DeleteIcon />} onClick={uninstallGame} />
          </Box>


        </Box>
        <div class="text-accent" style={{
          "font-family": "Roboto",
          "font-style": "normal",
          "font-weight": "400",
          "font-size": "12px",
          "line-height": "14px",
          "display": "flex",
          "margin-bottom": "14px",
        }}>Wi-Fi IPs (not public): {appInfo()?.browserIPs.join(", ") ?? "Loading"}</div>

        <Box sx={{ marginY: 3 }}>
          <OptionHeader>Permissions</OptionHeader>

          <Box sx={{
            display: "flex",
            gap: 2,
            alignItems: "center",
          }}>
            <RunButton text='Give permissions to game folder' icon={<PlayArrowRounded />} />
            <RunButton text='Allow manage storage permission' icon={<DeleteIcon />} />
          </Box>
        </Box>

        <Box sx={{ marginY: 3 }}>
          <OptionHeader >Login to Oculus for downloading</OptionHeader>

          <Typography component={"p"} sx={{
            fontFamily: 'Roboto',
            fontStyle: 'normal',
            fontWeight: 400,
            fontSize: "12px",
            lineHeight: "14px",
            display: "flex",
            alignItems: "center",
            color: "#D1D5DB",
          }}>Log in with your Oculus/Facebook account to downgrade games, browser login works on quest only</Typography>
          <Box sx={{
            display: "flex",
            gap: 2,
            alignItems: "center",
            marginTop: 1,
          }}>
            <RunButton text='Login using email and password' icon={<PlayArrowRounded />} disabled={!IsOnQuest()} />
            <RunButton text='Login using a token' icon={<DeleteIcon />} />
          </Box>
        </Box>

        <Box sx={{ marginY: 3 }}>
          <OptionHeader>Server control</OptionHeader>

          <Box sx={{
            display: "flex",
            gap: 0,
            alignItems: "center",
            marginTop: 1,
          }}>
            <TextField sx={{
              borderRadius: "222px",
              ".MuiInputBase-root": {
                borderRadius: "6px 0px 0px 6px",
              }
            }} size="small" variant="outlined" color="primary"
              value={port()}
              onChange={
                (e) => {
                  let value = e.currentTarget.value;
                  if (!value) return setPort(0)
                  if (isNaN(parseInt(value))) return;

                  setPort(parseInt(e.currentTarget.value))
                }
              } />
            <RunButton style={
              {
                height: "40px",
                width: "100px",
                "border-radius": "0px 6px 6px 0px",
              }
            } text='Change port' variant="success" onClick={changePortClick} />
          </Box>
        </Box>

      </div>
    </PageLayout>

  )
}
