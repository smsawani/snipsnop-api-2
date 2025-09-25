variable "environment_name" {
  description = "Name of the environment that can be used as part of naming resource convention."
  type        = string
  validation {
    condition     = length(var.environment_name) >= 1 && length(var.environment_name) <= 64
    error_message = "Environment name must be between 1 and 64 characters."
  }
}

variable "location" {
  description = "Primary location for all resources."
  type        = string
  validation {
    condition     = length(var.location) >= 1
    error_message = "Location must not be empty."
  }
}

variable "deployment_user_principal_id" {
  description = "Id of the principal to assign database and application roles."
  type        = string
  default     = ""
}

variable "service_name" {
  description = "Service name used as value for the tag (azd-service-name) azd uses to identify deployment host."
  type        = string
  default     = "api"
}

variable "resource_group_name" {
  description = "Name of the Azure resource group where resources will be created."
  type        = string
}