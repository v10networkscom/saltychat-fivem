import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { Config } from '../models/config.interface';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {

  private _config: BehaviorSubject<Config>;

  constructor() {
    this._config = new BehaviorSubject<Config>({
      isSpeakerEnabled: false,
      isMicClickEnabled: false,
      isPoweredOn: false,
      primaryChannel: null,
      radioVolume: 100,
      secondaryChannel: null,
    });
  }

  updateConfig(config: Config): void {
    this._config.next(config);
  }

  getConfig$(): Observable<Config> {
    return this._config.asObservable();
  }
}
