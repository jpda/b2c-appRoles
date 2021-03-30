export interface IDevice {
    deviceId: string;
    label: string;
    name: string;
    state: IDeviceState;
}

export class Device implements IDevice {
    deviceId: string;
    label: string;
    name: string;
    state: IDeviceState;

    constructor(deviceId: string, label: string, name: string, state: IDeviceState) {
        this.deviceId = deviceId;
        this.label = label;
        this.name = name;
        this.state = state;
    }
}

export interface IDeviceState {
    powerMeterInW: number;
    energyMeterInkWh: number;
}

export class DeviceState implements IDeviceState {
    powerMeterInW: number;
    energyMeterInkWh: number;

    constructor(powerMeterInW: number, energyMeterInkWh: number) {
        this.powerMeterInW = powerMeterInW;
        this.energyMeterInkWh = energyMeterInkWh;
    }
}