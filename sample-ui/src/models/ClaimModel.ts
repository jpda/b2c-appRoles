export interface IClaim {
    key: string,
    value: string
}

export class Claim implements IClaim {
    key: string;
    value: string;

    constructor(key: string, value: string) {
        this.key = key;
        this.value = value;
    }
}