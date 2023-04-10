import { Title } from "@solidjs/meta"
import "./ToolsPage.scss"
import { exitApp } from "../api/app"
import { showChangeGameModal } from "../modals/ChangeGameModal"
import PageLayout from "../Layouts/PageLayout"
import { Box, TextField, Typography } from "@suid/material"
import RunButton from "../components/Buttons/RunButton"
import { DeleteIcon, UploadRounded } from "../assets/Icons"
import PlayArrowRounded from "@suid/icons-material/PlayArrowRounded"

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
  return (
    <PageLayout>
      <div class=" contentItem toolsPage">
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
            <RunButton text='Run Game' variant="success" icon={<PlayArrowRounded />} />
            <RunButton text='Uninstall game' icon={<DeleteIcon />} />
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

        }}>Wi-Fi IPs (not public): http://10.0.0.5:50002, http://10.0.0.5:50002, http://10.0.0.5:50002</div>

        <Box sx={{marginY: 3}}>
          <OptionHeader >Permissions</OptionHeader>
          
          <Box sx={{
            display: "flex",
            gap: 2,
            alignItems: "center",
          }}>
            <RunButton text='Give permissions to game folder' icon={<PlayArrowRounded />} />
            <RunButton text='Allow manage storage permission' icon={<DeleteIcon />} />
          </Box>
        </Box>

        <Box  sx={{marginY: 3}}>
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
            <RunButton text='Login using email and password' icon={<PlayArrowRounded />} />
            <RunButton text='Login using a token' icon={<DeleteIcon />} />
          </Box>
        </Box>

        <Box  sx={{marginY: 3}}>
          <OptionHeader>Server control</OptionHeader>
          
          <Box sx={{
            display: "flex",
            gap: 0,
            alignItems: "center",
            marginTop: 1,
          }}>
            <TextField sx={{
              borderRadius: "222px",
              ".MuiInputBase-root":  {
                borderRadius: "6px 0px 0px 6px",
              }
            }} size="small"  placeholder="Port" variant="outlined" color="primary" />
            <RunButton style={
              {
                height: "40px",
                width: "100px",
                "border-radius": "0px 6px 6px 0px",
              }
            } text='Change port' variant="success" />
          </Box>
        </Box>
       
      </div>
    </PageLayout>

  )
}
