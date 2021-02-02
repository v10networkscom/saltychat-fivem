import { Component } from '@angular/core';
import { ConfigService } from './config/service/config.service';
import { NuiEventService } from './fivem/services/nui-event.service';
import { Config } from './config/models/config.interface';

@Component({
  selector: 'salty-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {

  constructor(
    private configService: ConfigService,
    private nuiEventService: NuiEventService
  ) {
    this.nuiEventService.postRequest<Config>('ready').subscribe({
      next: config => {
        this.configService.updateConfig(config);
      }
    });
  }

}
