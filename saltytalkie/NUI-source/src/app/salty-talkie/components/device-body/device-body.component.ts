import { Component, HostListener, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { NuiEventService } from '../../../fivem/services/nui-event.service';

@Component({
  selector: 'salty-device-body',
  templateUrl: './device-body.component.html',
  styleUrls: ['./device-body.component.scss']
})
export class DeviceBodyComponent implements OnInit {


  speakerIsActive = false;
  isPoweredOn = false;

  currentVolume = 50;

  channels: FormGroup;

  constructor(private fb: FormBuilder,
              private nuiEventService: NuiEventService) {
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
    this.nuiEventService.postRequest('ready').subscribe({
      next: value => {
        console.log(value);
        // this.nuiEventService.messages$().subscribe({next: value => console.log('msg', value)});
      }
    });

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
}
