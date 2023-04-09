import { Title } from "@solidjs/meta";
import PageLayout from "../Layouts/PageLayout";

export default function DownloadProgressPage() {
  return (
    <PageLayout>
      <div class="contentItem">
        <Title>Downloads</Title>
        <div id="progressBarContainers" style="width: 95%;">

        </div>
      </div>
    </PageLayout>

  )
}
