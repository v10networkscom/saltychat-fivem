export interface Config {
  isSpeakerEnabled: boolean;
  isMicClickEnabled: boolean;
  isPoweredOn: boolean;
  primaryChannel: string | null;
  radioVolume: number;
  secondaryChannel: string | null;
}
