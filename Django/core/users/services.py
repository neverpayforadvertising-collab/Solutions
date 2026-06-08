# users/services.py
from core.users.models import User

def get_users():
    return User.objects.all()