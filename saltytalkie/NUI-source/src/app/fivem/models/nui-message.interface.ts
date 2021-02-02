import { NuiMessageType } from '../enums/nui-message-type.enum';

export interface NuiMessage<T = any> {
  messageType: NuiMessageType;
  body: T;
}
