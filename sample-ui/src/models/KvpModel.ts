export interface IKvp {
    key: string,
    value: string
}

export class Kvp implements IKvp {
    key: string;
    value: string;

    constructor(key: string, value: string) {
        this.key = key;
        this.value = value;
    }
}