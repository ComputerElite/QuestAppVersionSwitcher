import { Title, Link, Meta } from '@solidjs/meta';
import { config } from '../store';
import { backupList } from '../state/backups';
import { showChangeGameModal } from '../modals/ChangeGameModal';
import { For, createEffect, createSignal, on } from 'solid-js';
import { Box, Button, IconButton, List, ListItem, Typography } from '@suid/material';
import { IBackup } from '../api/backups';
import { FaSolidWindowRestore } from 'solid-icons/fa';
import { FiPlus, FiTrash } from 'solid-icons/fi'
const [selectedBackup, setSelectedBackup] = createSignal<IBackup | null>(null);
import { OcPencil3 } from 'solid-icons/oc'
import { showConfirmModal } from '../modals/ConfirmModal';
export default function BackupPage() {

 
    // Select first backup if there are any backups and remove selection if there are no backups
    createEffect(on(backupList, (backup) => {
        if (backup == null || backup.backups == null || backup.backups.length == 0) {
            return setSelectedBackup(null);
        }
        if (selectedBackup() == null) {
            setSelectedBackup(backup.backups[0]);
        }
    }))

    async function onDeleteClick(backup: IBackup) {
        console.log("delete");
        let confirm = await showConfirmModal({
            title: "Delete backup",
            message: `Are you sure you want to delete the backup "${backup.backupName}"?`,
            okText: "Delete",
            cancelText: "Cancel",
        })

        if (confirm) {
            console.log("delete confirmed");
        };
    }

    return (
        <div class='contentItem'>
            <Title>Backup</Title>

            <Box sx={{
                display: "flex",
                alignItems: "flex-end",
                width: "100%",
                // justifyContent: "space-between",
                flexWrap: "wrap",
            }}>
                <Box sx={{
                    display: "flex",
                }}>
                    <Typography variant="h5" component="h2" sx={{ color: "white" }}>
                        Backups of
                    </Typography>
                    <Typography variant="h5" component="h2" sx={{ color: "#F9F", ml: 1 }} onClick={showChangeGameModal}>
                        {config()?.currentApp ?? "change app"}
                    </Typography>
                </Box>
                <Box sx={{
                    display: "flex",
                    color: "#F9F",
                    
                    
                }}>
                    <IconButton sx={{

                        color: "#F9F",
                    }} onClick={showChangeGameModal}>
                        <OcPencil3 />
                    </IconButton>
                    {/* <Button endIcon={<FaSolidWindowRestore />} color='primary' onClick={showChangeGameModal}>Switch app</Button>
                    <Button endIcon={<FaSolidWindowRestore />} color='primary' onClick={showChangeGameModal}>Switch app</Button>
                    <Button endIcon={<FaSolidWindowRestore />} color='primary' onClick={showChangeGameModal}>Switch app</Button> */}
                </Box>
            </Box>

            <div class="topMargin">Backups of <div class="inline packageName">{config()?.currentApp ?? "some app"}</div> (<div id="size" class="inline">{backupList()?.backupsSizeString}</div>)</div>
            <List sx={{
                width: '100%',
                height: '100%',
                overflowX: 'auto',
                minHeight: '100px',
                maxHeight: '300px',
            }}>
                <For each={backupList()?.backups}>
                    {(backup) => (
                        <ListItem sx={{
                            cursor: "pointer",
                            display: "flex",
                            flexDirection: "row",
                            alignItems: "flex-start",
                            backgroundColor: selectedBackup()?.backupName == backup.backupName ? "#333" : "#222",
                            justifyContent: "space-between",
                        }}
                            onClick={() => setSelectedBackup(backup)}
                        >
                            <Box>
                                <Typography component={"div"} sx={{ color: "white" }} >
                                    {backup.backupName}
                                </Typography>
                                <Typography component={"div"} fontSize={"0.9em"} sx={{ color: "pink" }} >
                                    {backup.backupSizeString}
                                </Typography>

                            </Box>

                            <Box>
                                <IconButton color='error' onClick={()=> onDeleteClick(backup)}>
                                    <FiTrash />
                                </IconButton>
                            </Box>
                        </ListItem>
                    )}
                </For>
            </List>


            {/* <div class="contentHeader headerMargin">
                Restore backups
                <div class="contentHeaderDescription">Restores the backup selected above</div>
                <br />
                <b class="hidden" id="onPcInfo">You can only restore backups in headset</b>
                <label><input type="checkbox" id="restoreAppData" value="only restore app data" style="width: auto;" />Only restore app data</label>
                <div class="buttonContainer">
                    <div class="button" id="restoreBackup">Restore backup</div>
                    <div class="buttonLabel">Restores the selected backup</div>
                </div>
                <div class="buttonContainer">
                    <div class="button" id="deleteBackup">Delete backup</div>
                    <div class="buttonLabel">Deletes the selected backup</div>
                </div>
                <div id="restoreTextBox" class="textBox"></div>
            </div> */}

            {/* <div class="contentHeader headerMargin">
                Create backups
                <div class="contentHeaderDescription">Change the app at the top</div>
            </div>
            <div>
                <input type="text" placeholder="Backup Name" id="backupname" />
                <div class="buttonContainer">
                    <div class="button" id="createBackup">Create backup</div>
                    <div class="buttonLabel">Creates a backup of <div class="inline packageName">some game</div></div>
                </div>
                <label><input type="checkbox" id="appdata" value="only backup app data" style="width: auto;" />Only backup app data</label>
                <div id="backupTextBox" class="textBox"></div>
            </div> */}
        </div>
    )
}


function BackupItem() {
    return (
        <div class="listItem">
            <div class="listItemName"></div>
            <div class="listItemSize"></div>
            <div class="listItemDate"></div>
        </div>
    )
}

// fetch("/api/backups?package=" + config.currentApp).then(res => res.json().then(res => {
//     document.getElementById("backupList").innerHTML = ""
//     document.getElementById("size").innerHTML = res.backupsSizeString
//     if (res.backups) {
//         res.backups.forEach(backup => {
//             document.getElementById("backupList").innerHTML += `<div class="listItem${backup.backupName == selectedBackup ? " listItemSelected" : ""}" value="${backup.backupName}">${backup.backupName} (${backup.backupSizeString})</div>`
//         })
//     }
//     if (document.getElementById("backupList").innerHTML == "") document.getElementById("backupList").innerHTML = `<div class="listItem" value="">No Backups</div>`
//     Array.prototype.forEach.call(document.getElementsByClassName("listItem"), i => {
//         i.onclick = function () {
//             selectedBackup = i.getAttribute("value")
//             UpdateUI()
//         }
//     })
// }))