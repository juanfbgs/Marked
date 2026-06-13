import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { User } from "@core/models/auth.model";
import { environment } from "@envs/environment.development";
import { map, Observable } from "rxjs";

@Injectable({
    providedIn: 'root',
})
export class ProfileService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.API_URL}/user`;

    getProfile(): Observable<User> {
        return this.http.get<{ user: User }>(`${this.baseUrl}/profile`).pipe(
            map((res) => res.user),
        );
    }
}