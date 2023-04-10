import { Title } from "@solidjs/meta";
import PageLayout from "../Layouts/PageLayout";
import { Box } from "@suid/material";
import RunButton from "../components/Buttons/RunButton";
import { UploadRounded } from "../assets/Icons";
import PlayArrowRounded from "@suid/icons-material/PlayArrowRounded";

export default function BackupPage() {
  return (
    <PageLayout>
      <div class="contentItem">
        <Title>Cosmetics QAVS</Title>
        <Box sx={{
          display: "flex",
          width: "100%",
          gap: 1,
          flexWrap: "wrap",
          justifyContent: "space-between",
          marginBottom: 2,
        }}>
          <Box sx={{
            display: "flex",
            gap: 2,
            alignItems: "center",
          }}>
            <RunButton text='Launch Game' variant="success" icon={<PlayArrowRounded />} />
            <RunButton text='Upload a cosmetic' icon={<UploadRounded />} />
            <span style={{
              "font-family": "Roboto",
              "font-style": "normal",
              "font-weight": "400",
              "font-size": "12px",
              "line-height": "14px",
              "display": "flex",
              "align-items": "center",
              "text-align": "center",
              "color": "#D1D5DB",
              "margin-left": "10px",
            }} class="text-accent" >
              Get more cosmetics
            </span>
          </Box>

          <Box sx={{
            display: "flex",
            gap: 2,
            alignItems: "center",
          }}>
            <RunButton text='Delete all' onClick={() => { }} style={"width: 80px"} />
          </Box>
        </Box>
      </div>
    </PageLayout>

  )
}
