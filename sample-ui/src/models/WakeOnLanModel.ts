export interface IWakeOnLanServer {
    key: string,
    value: string
}

export class WakeOnLanServer implements IWakeOnLanServer {
    key: string;
    value: string;

    constructor(key: string, value: string) {
        this.key = key;
        this.value = value;
    }
}