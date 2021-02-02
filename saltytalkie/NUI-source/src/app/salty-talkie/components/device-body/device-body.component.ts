import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { NuiEventService } from '../../../fivem/services/nui-event.service';
import { ConfigService } from '../../../config/service/config.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'salty-device-body',
  templateUrl: './device-body.component.html',
  styleUrls: ['./device-body.component.scss']
})
export class DeviceBodyComponent implements OnInit, OnDestroy {


  speakerIsActive = false;
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
          this.channels.controls.primary.setValue(config.secondaryChannel);
          this.isPoweredOn = config.isPoweredOn;
          this.speakerIsActive = config.isSpeakerEnabled;
          this.isMicClickEnabled = config.isMicClickEnabled;
        }
      })
    );
  }

  toggleSpeaker(): void {
    this.speakerIsActive = !this.speakerIsActive;
  }

  addNumberToChannel(numberToAdd: number): void {
    this.channels.controls.primary.setValue('' + this.channels.controls.primary.value + numberToAdd);
  }

  togglePower(): void {
    this.isPoweredOn = !this.isPoweredOn;
  }


  turnVolumeUp(): void {
    if (!(this.currentVolume + 10 > 160)) {
      this.currentVolume += 10;
    }
  }

  turnVolumeDown(): void {
    if (!(this.currentVolume - 10 < 0)) {
      this.currentVolume -= 10;
    }
  }

  unfocus(): void {
    this.nuiEventService.postRequest('unfocus').subscribe();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }
}
