import { Location } from '@angular/common';
import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { AccountService } from 'src/app/_services/account.service';

@Component({
    selector: 'app-member-password',
    templateUrl: './member-password.component.html',
    styleUrls: ['./member-password.component.css']
})
export class MemberPasswordComponent implements OnInit {
    @Output() cancelChangePassword = new EventEmitter();
    passwordForm: FormGroup;
    validationErrors: string[] = [];
    completed: boolean;

    constructor(
        private accountService: AccountService,
        private fb: FormBuilder,
        private location: Location
    ) { }

    ngOnInit(): void {
        this.initializeForm();
    }

    initializeForm() {
        this.passwordForm = this.fb.group({
            currentPassword: ['', [Validators.required]],
            newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(20)]],
            confirmPassword: ['', [Validators.required, this.matchValues('newPassword')]]
        });
        this.passwordForm.controls.newPassword.valueChanges.subscribe(() => {
            this.passwordForm.controls.confirmPassword.updateValueAndValidity();
        });
    }

    matchValues(matchTo: string): ValidatorFn {
        return (control: AbstractControl) => {
            return control?.value === control?.parent?.controls[matchTo].value
                ? null : { isMatching: true }
        }
    }

    register() {
        this.accountService.changePassword(this.passwordForm.value).subscribe(response => {
            this.completed = true;
            this.passwordForm.reset();
        }, error => {
            this.validationErrors = error;
        });
    }

    cancel() {
        this.location.back();
    }

}
