import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DeviceBodyComponent } from './components/device-body/device-body.component';
import {ReactiveFormsModule} from '@angular/forms';



@NgModule({
  declarations: [DeviceBodyComponent],
  exports: [
    DeviceBodyComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule
  ]
})
export class SaltyTalkieModule { }
