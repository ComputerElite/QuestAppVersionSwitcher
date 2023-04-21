import { createStore } from "solid-js/store";
import { ModTask, QAVSModOperationType, getModOperations } from "../api/mods";
import { createSignal } from "solid-js";
import toast from "solid-toast";
import { refetchModdingStatus } from "../store";
import { refetchMods } from "./mods";



/**
 * The task manager is used to keep track of all the tasks that are currently running.
 * 
 * We have a list of tasks, and we can add, remove, get, and get all tasks.
 */

let [tasks, setTasks] = createStore<ModTask[]>([]);

type TaskType = "tasks-done" | "task-done" | "task-new";

class BackendEventsClass extends EventTarget {
    constructor() {
        super();
    }

    emitTasksDone() {
        const event = new CustomEvent('tasks-done');
        this.dispatchEvent(event);
    }

    emitTaskDone(task: ModTask) {
        const event = new CustomEvent('task-done', { detail: task });
        this.dispatchEvent(event);
    }

    emitNewTask(task: ModTask) {
        const event = new CustomEvent('task-new', { detail: task });
        this.dispatchEvent(event);
    }

    addEventListener(type: TaskType, callback: EventListenerOrEventListenerObject | null, options?: boolean | AddEventListenerOptions | undefined): void {
        super.addEventListener(type, callback, options);
    }

    removeEventListener(type: TaskType, callback: EventListenerOrEventListenerObject | null, options?: boolean | EventListenerOptions | undefined): void {
        super.removeEventListener(type, callback, options);
    }
}

export const BackendEvents = new BackendEventsClass();

let [operationsInProgress, setOperationsInProgress] = createSignal<boolean>(false);

export function addTask(task: ModTask) {
    setTasks([...tasks, task]);
}

export function removeTask(operationId: number) {
    setTasks(tasks.filter((task) => task.operationId !== operationId));
}

export function getTask(operationId: number) {
    return tasks.find((task) => task.operationId === operationId);
}

export function getTasks() {
    return tasks;
}

export async function refetchTasks() {
    try {
        let resp: ModTask[] = await getModOperations();

        // Flags 
        let hasTasksInProgress = false;

        let oldTasks = tasks;

        // Analyze tasks that we have and tasks that we got from the backend
        resp.forEach((task) => {
            // If we have a task that is not in the list, add it
            if (!hasTasksInProgress && !task.isDone) {
                hasTasksInProgress = true;
            }

            // Compare status of old task and new task
            let oldTask = getTask(task.operationId);
            if (!oldTask) {
                if (task.isDone) {
                    hasTasksInProgress = true;
                }
                BackendEvents.emitNewTask(task);
            } else {
                // We assume that the task cannot go from done to not done
                if (oldTask.isDone !== task.isDone) {
                    BackendEvents.emitTaskDone(task);
                }
            }
        });

        // Track operations in progress
        if (!hasTasksInProgress && operationsInProgress()) {
            setOperationsInProgress(false);
            BackendEvents.emitTasksDone();
        } else if (hasTasksInProgress && !operationsInProgress()) {
            setOperationsInProgress(true);
        }



        // Set the tasks
        setTasks(resp);
    } catch (e) {
        console.error(e);
    }
}

let refetchingInterval: NodeJS.Timer | null = null;
export function startRefetchingModTasks() {

    console.log("Starting refetching mod tasks");
    // If we are already refetching, don't do anything
    if (refetchingInterval) return;
    console.log("Starting refetching mod tasks");
    refetchingInterval = setInterval(async () => {
        await refetchTasks();
        if (!operationsInProgress()) {
            refetchingInterval && clearInterval(refetchingInterval);
        }
    }
        , 1000);
}

BackendEvents.addEventListener("task-done", async (e) => {
    let task = (e as CustomEvent).detail as ModTask;

    if (task.type == QAVSModOperationType.Error) {
        toast.error(`${task.name}`);
    } else {
        toast.success(`${task.name} `);
    }
    await refetchModdingStatus();
    await refetchMods();
});

BackendEvents.addEventListener("task-new", async (e) => {
    let task = (e as CustomEvent).detail as ModTask;
    // toast(`${task.name}`);
});


BackendEvents.addEventListener("tasks-done", async (e) => {
    toast.success(`All tasks are done!`);
    await refetchModdingStatus();
    await refetchMods();
});