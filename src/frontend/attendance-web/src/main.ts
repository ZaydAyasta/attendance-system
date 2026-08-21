import Aura from '@primevue/themes/aura'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import { createApp } from 'vue'
import 'primeicons/primeicons.css'
import App from './App.vue'
import router from './router'
import './style.css'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(ToastService)
app.use(PrimeVue, {
  ripple: false,
  theme: {
    preset: Aura,
  },
})

app.mount('#app')
