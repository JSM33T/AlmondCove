﻿// main.ts
import { Email } from '../Interfaces/email.interface';
import { validateEmail, postData } from '../global.js';

document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('.subscription-form') as HTMLFormElement;
    form.addEventListener('submit', function (event) {
        event.preventDefault();
        const emailInput = document.getElementById('subscr-email') as HTMLInputElement;
        const emailData = {
            email: emailInput.value,
            origin: "HomePage",
        };
        postEmailToAPI(emailData);
    });
});

async function postEmailToAPI(emailData: Email) {

    if (!validateEmail(emailData.email)) {
        alert("invalid email");
        return;
    }
    const submitBtn = document.getElementById('submitMail') as HTMLButtonElement;
    submitBtn.textContent = "loading";
    const apiUrl = '/api/mailinglist/subscribe';
    try {
        postData(apiUrl, emailData)
            .then((result) => {
                console.log('Result:', result);
            })
            .catch((error) => {
                console.error('Error:', error);
            });   

    } catch (error: any) {
        if (error.response.data) {

        }
    }
    finally {
        submitBtn.textContent = "Subscribe";
    }
}