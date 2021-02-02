import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { NuiEventService } from '../../../fivem/services/nui-event.service';
import { ConfigService } from '../../../config/service/config.service';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { NuiMessageType } from '../../../fivem/enums/nui-message-type.enum';

@Component({
  selector: 'salty-device-body',
  templateUrl: './device-body.component.html',
  styleUrls: ['./device-body.component.scss']
})
export class DeviceBodyComponent implements OnInit, OnDestroy {


  isSpeakerActive = false;
  isPoweredOn = false;
  isMicClickEnabled = false;

  currentVolume = 50;

  channels: FormGroup;

  subscriptions: Subscription[] = [];

  constructor(private fb: FormBuilder,
              private nuiEventService: NuiEventService,
              private configService: ConfigService) {
    this.channels = this.fb.group({
      primary: [''],
      secondary: ['']
    });
  }

  @HostListener('window:keyup', ['$event'])
  keyEvent(event: KeyboardEvent): void {

    const regex = /^(Digit|Numpad)(\d)$/s;
    const m = regex.exec(event.code);

    if (m !== null) {

      if (m[2]) {
        this.addNumberToChannel(+m[2]);
      }
    }
  }

  ngOnInit(): void {
    this.subscriptions.push(this.configService.getConfig$().subscribe({
        next: config => {
          this.currentVolume = config.radioVolume;
          this.channels.controls.primary.setValue(config.primaryChannel);
          this.channels.controls.secondary.setValue(config.secondaryChannel);
          this.isPoweredOn = config.isPoweredOn;
          this.isSpeakerActive = config.isSpeakerEnabled;
          this.isMicClickEnabled = config.isMicClickEnabled;
        }
      })
    );

    this.subscriptions.push(
      this.nuiEventService.messages$().pipe(
        filter(message => message.messageType === NuiMessageType.SETPRIMARYRADIOCHANNEL)).subscribe(
        {
          next: message => {
            if (message.body.channelName) {
              this.channels.controls.primary.setValue(message.body.channelName.replace('st_', ''));
            }
          }
        }
      )
    );

    this.subscriptions.push(
      this.nuiEventService.messages$().pipe(
        filter(message => message.messageType === NuiMessageType.SETSECONDARYCHANNEL)).subscribe(
        {
          next: message => {
            if (message.body.channelName) {
              this.channels.controls.secondary.setValue(message.body.channelName.replace('st_', ''));
            }
          }
        }
      )
    );


  }


  setPrimaryChannel(newChannel: string): void {
    this.nuiEventService.postRequest<string>('setPrimaryChannel', newChannel).subscribe();
  }


  setSecondaryChannel(newChannel: string): void {
    this.nuiEventService.postRequest<string>('setSecondaryChannel', newChannel).subscribe();
  }

  toggleMicClick(): void {
    this.nuiEventService.postRequest<boolean>('toggleMicClick').subscribe({
      next: isMicClickEnabled => {
        this.isMicClickEnabled = isMicClickEnabled;
      }
    });
  }

  toggleSpeaker(): void {
    this.nuiEventService.postRequest<boolean>('toggleSpeaker').subscribe({
      next: isSpeakerActive => {
        this.isSpeakerActive = isSpeakerActive;
      }
    });
  }

  addNumberToChannel(numberToAdd: number): void {
    this.channels.controls.primary.setValue('' + this.channels.controls.primary.value + numberToAdd);
  }

  togglePower(): void {
    this.nuiEventService.postRequest<boolean>('togglePower').subscribe({
      next: isPoweredOn => {
        this.isPoweredOn = isPoweredOn;
      }
    });
  }

  turnVolumeUp(): void {
    this.nuiEventService.postRequest<number>('radioVolumeUp').subscribe({
      next: newVolume => {
        console.log(newVolume);
        this.currentVolume = newVolume;
      }
    });
  }

  turnVolumeDown(): void {
    this.nuiEventService.postRequest<number>('radioVolumeDown').subscribe({
      next: newVolume => {
        console.log(newVolume);
        this.currentVolume = newVolume;
      }
    });
  }

  unfocus(): void {
    this.nuiEventService.postRequest('unfocus').subscribe();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }
}
