from rest_framework import serializers
from .models import User

class UserSerializer(serializers.ModelSerializer):
    class Meta:
        model = User
        fields = "__all__"
        
    def validate_email(self, value):
        if "@" not in value:
            raise serializers.ValidationError("Invalid email")
        return value
            
