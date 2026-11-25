import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AiChatService, ChatTurn } from '../../core/services/ai-chat.service';

@Component({
  selector: 'app-ai-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-chat.component.html'
})
export class AiChatComponent {
  public messages: ChatTurn[] = [{
    role: 'assistant',
    content: 'Hi! I can help with current blood-stock levels and upcoming donation camps. What would you like to know?'
  }];
  public draft = '';
  public isSending = false;
  public errorMessage = '';
  public dataAsOfUtc: string | null = null;

  constructor(private readonly chatService: AiChatService) {}

  public send(): void {
    const message = this.draft.trim();
    if (!message || this.isSending) return;

    const history = this.messages.slice(-8);
    this.messages = [...this.messages, { role: 'user', content: message }];
    this.draft = '';
    this.errorMessage = '';
    this.isSending = true;

    this.chatService.ask(message, history).subscribe({
      next: (response) => {
        this.messages = [...this.messages, { role: 'assistant', content: response.answer }];
        this.dataAsOfUtc = response.dataAsOfUtc;
        this.isSending = false;
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage = this.getErrorMessage(error);
        this.isSending = false;
      }
    });
  }

  public clearConversation(): void {
    this.messages = [{
      role: 'assistant',
      content: 'Hi! I can help with current blood-stock levels and upcoming donation camps. What would you like to know?'
    }];
    this.dataAsOfUtc = null;
    this.errorMessage = '';
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    const serverMessage = error.error?.detail ?? error.error?.message ?? error.error?.title;
    if (typeof serverMessage === 'string') return serverMessage;
    return 'The assistant could not respond. Check that the backend and local Ollama model are running, then try again.';
  }
}
