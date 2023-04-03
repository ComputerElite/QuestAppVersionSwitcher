import { Title } from "@solidjs/meta";

export default function BackupPage() {
  return (
    <div class="contentItem">
      <Title>Cosmetics QAVS</Title>
      <div class="buttonContainer"><div class="button" >Launch Game</div></div>
      <div class="buttonContainer"><div class="button" id="installCosmeticButton" >Install a Cosmetic from Disk</div></div>
      <h2>Cosmetics type</h2>
      <select id="cosmeticsType">

      </select>
      <div id="availableAfterModdingTypes" style="display: none;">Types available after modding: </div>
      <div class="infiniteList topMargin" id="cosmeticsList">

      </div>
    </div>
  )
}
