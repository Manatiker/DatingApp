import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ReplaySubject } from 'rxjs';
import {map} from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { User } from '../_models/user';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  baseurl = environment.apiUrl;
  private currentUserSource = new ReplaySubject<User>(1);
  currentUser$ = this.currentUserSource.asObservable();

  constructor(private http: HttpClient) { }

  login(model: any){
    return this.http.post(this.baseurl + 'account/login', model).pipe(
      map((response: User) => {
        const user = response;
        if (user) {
         this.setCurrentUser(user);
        }
      })
    )
  }

  register(model: any) {
    return this.http.post(this.baseurl + 'account/register', model).pipe(
      map((user: User) => {
        if(user) {
         this.setCurrentUser(user);
        }
      })
    )
  }
/* TAK DZIAŁA FUNKCJA ANONIMOWA |
                                V
   register2(model: any) {
    this.http.post(this.baseurl + 'account/register', model).subscribe(
      (user:User) => {
        if(user) {
          localStorage.setItem('user', JSON.stringify(user));
          this.currentUserSource.next(user);
        }
      }
    )
  }

  TUTAJ PRZYKŁAD JAK WYGLĄDAŁABY FUNKCJA REGISTER2 BEZ FUNKCJI ANONIMOWEJ CZYLI " => "

  register2(model: any) {
    this.http.post(this.baseurl + 'account/register', model).subscribe(this.setItUp)
  }

  setItUp(user: User) {

      if(user) {
        localStorage.setItem('user', JSON.stringify(user));
        this.currentUserSource.next(user);
      }

  }
*/
  setCurrentUser(user: User) {
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUserSource.next(user);
  }

  logout(){
    localStorage.removeItem('user');
    this.currentUserSource.next(null);
  }
}
