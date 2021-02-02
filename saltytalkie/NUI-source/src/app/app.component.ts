import { Component, OnDestroy, OnInit } from '@angular/core';
import { ConfigService } from './config/service/config.service';
import { NuiEventService } from './fivem/services/nui-event.service';
import { Config } from './config/models/config.interface';
import { Subscription } from 'rxjs';

@Component({
  selector: 'salty-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {

  private _subscriptions: Subscription[] = [];

  constructor(
    private configService: ConfigService,
    private nuiEventService: NuiEventService
  ) {
    this.nuiEventService.postRequest<Config>('ready', null, true).subscribe({
      next: config => {
        this.configService.updateConfig(config);
      }
    });
  }

  ngOnInit(): void {
    this._subscriptions.push(this.nuiEventService.messages$().subscribe(m => console.log(m)));
  }

  ngOnDestroy(): void {
    this._subscriptions.forEach(sub => sub.unsubscribe());
  }

}
